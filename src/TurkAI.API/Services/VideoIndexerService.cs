using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using TurkAI.Shared.Models;

namespace TurkAI.API.Services;

/// <summary>
/// Azure Video Indexer integration — submits a video URL for indexing and retrieves
/// AI-extracted insights (scenes, keywords, destinations, transcripts).
/// Uses the Azure Video Indexer REST API since there is no stable .NET SDK package.
/// </summary>
public sealed class VideoIndexerService : IVideoIndexerService
{
    private readonly HttpClient _httpClient;
    private readonly string _accountId;
    private readonly string _location;
    private readonly string _subscriptionKey;
    private readonly ILogger<VideoIndexerService> _logger;

    private const string ApiBaseUrl = "https://api.videoindexer.ai";

    public VideoIndexerService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<VideoIndexerService> logger)
    {
        _accountId = configuration["AzureVideoIndexer:AccountId"]
            ?? throw new InvalidOperationException("AzureVideoIndexer:AccountId is not configured.");
        _location = configuration["AzureVideoIndexer:Location"]
            ?? throw new InvalidOperationException("AzureVideoIndexer:Location is not configured.");
        _subscriptionKey = configuration["AzureVideoIndexer:SubscriptionKey"]
            ?? throw new InvalidOperationException("AzureVideoIndexer:SubscriptionKey is not configured.");

        _httpClient = httpClientFactory.CreateClient("VideoIndexer");
        _logger = logger;
    }

    public async Task<VideoInsights> IngestVideoAsync(VideoIngestionRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Ingesting video: {Url}", request.VideoUrl);

        var accessToken = await GetAccessTokenAsync(cancellationToken);

        var uploadUrl = $"{ApiBaseUrl}/{_location}/Accounts/{_accountId}/Videos" +
                        $"?accessToken={Uri.EscapeDataString(accessToken)}" +
                        $"&name={Uri.EscapeDataString(!string.IsNullOrEmpty(request.Name) ? request.Name : "TurkAI-Video")}" +
                        $"&videoUrl={Uri.EscapeDataString(request.VideoUrl)}" +
                        $"&language={MapLanguage(request.Language)}" +
                        "&privacy=Private" +
                        "&indexingPreset=Default";

        var response = await _httpClient.PostAsync(uploadUrl, null, cancellationToken);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(body);
        var videoId = doc.RootElement.GetProperty("id").GetString()
                      ?? throw new InvalidOperationException("Video indexer did not return a video ID.");

        _logger.LogInformation("Video {VideoId} submitted for indexing", videoId);

        // Return initial insights — caller can poll GetVideoInsightsAsync
        return new VideoInsights
        {
            VideoId = videoId,
            Summary = "Video submitted for indexing. Call GetVideoInsightsAsync to retrieve full insights once processing is complete.",
            Scenes = [],
            Keywords = [],
            Destinations = [],
            Transcript = null
        };
    }

    public async Task<VideoInsights?> GetVideoInsightsAsync(string videoId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching insights for video {VideoId}", videoId);

        var accessToken = await GetAccessTokenAsync(cancellationToken);

        var indexUrl = $"{ApiBaseUrl}/{_location}/Accounts/{_accountId}/Videos/{videoId}/Index" +
                       $"?accessToken={Uri.EscapeDataString(accessToken)}";

        var response = await _httpClient.GetAsync(indexUrl, cancellationToken);
        if (!response.IsSuccessStatusCode) return null;

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(body);

        var state = doc.RootElement.TryGetProperty("state", out var s) ? s.GetString() : null;
        if (state is not "Processed") return null;

        var insights = doc.RootElement.TryGetProperty("videos", out var videos)
            ? videos.EnumerateArray().FirstOrDefault().TryGetProperty("insights", out var ins) ? ins : default
            : default;

        var keywords = ExtractStringList(insights, "keywords", "text");
        var scenes = ExtractStringList(insights, "scenes", "shots");
        var transcript = ExtractTranscript(insights);

        return new VideoInsights
        {
            VideoId = videoId,
            Summary = $"Video indexed successfully. {keywords.Count} keywords extracted.",
            Keywords = keywords,
            Scenes = scenes,
            Destinations = keywords.Where(k => IsLikelyDestination(k)).ToList(),
            Transcript = transcript
        };
    }

    private async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        var tokenUrl = $"{ApiBaseUrl}/auth/{_location}/Accounts/{_accountId}/AccessToken?allowEdit=false";
        var tokenRequest = new HttpRequestMessage(HttpMethod.Get, tokenUrl);
        tokenRequest.Headers.Add("Ocp-Apim-Subscription-Key", _subscriptionKey);

        var response = await _httpClient.SendAsync(tokenRequest, cancellationToken);
        response.EnsureSuccessStatusCode();

        var token = await response.Content.ReadAsStringAsync(cancellationToken);
        return token.Trim('"');
    }

    private static string MapLanguage(string lang) => lang switch
    {
        "tr" => "Turkish",
        "en" => "English",
        _ => "Auto"
    };

    private static List<string> ExtractStringList(JsonElement root, string arrayProp, string textProp)
    {
        if (!root.ValueKind.Equals(JsonValueKind.Object)) return [];
        if (!root.TryGetProperty(arrayProp, out var arr)) return [];
        return arr.EnumerateArray()
            .Select(e => e.TryGetProperty(textProp, out var t) ? t.GetString() ?? "" : "")
            .Where(s => s.Length > 0)
            .ToList();
    }

    private static string? ExtractTranscript(JsonElement root)
    {
        if (!root.ValueKind.Equals(JsonValueKind.Object)) return null;
        if (!root.TryGetProperty("transcript", out var transcript)) return null;
        var lines = transcript.EnumerateArray()
            .Select(e => e.TryGetProperty("text", out var t) ? t.GetString() ?? "" : "")
            .Where(s => s.Length > 0);
        return string.Join(" ", lines);
    }

    private static bool IsLikelyDestination(string keyword)
    {
        // Simple heuristic: starts with uppercase and is at least 4 chars
        return keyword.Length >= 4 && char.IsUpper(keyword[0]);
    }
}
