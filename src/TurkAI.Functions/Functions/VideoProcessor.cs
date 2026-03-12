using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using TurkAI.Shared.Models;

namespace TurkAI.Functions.Functions;

/// <summary>
/// Azure Function that processes video URL ingestion requests.
/// Accepts a VideoIngestionRequest and submits it to Azure Video Indexer
/// for background processing.
/// </summary>
public sealed class VideoProcessor
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<VideoProcessor> _logger;

    private const string ApiBaseUrl = "https://api.videoindexer.ai";

    public VideoProcessor(IHttpClientFactory httpClientFactory, ILogger<VideoProcessor> logger)
    {
        _httpClient = httpClientFactory.CreateClient("VideoIndexer");
        _logger = logger;
    }

    /// <summary>HTTP trigger — submit a video for indexing.</summary>
    [Function("IngestVideo")]
    public async Task<HttpResponseData> IngestVideoAsync(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "video/ingest")] HttpRequestData req,
        FunctionContext executionContext)
    {
        _logger.LogInformation("Video ingestion trigger fired");

        var body = await req.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(body))
        {
            var badReq = req.CreateResponse(HttpStatusCode.BadRequest);
            await badReq.WriteStringAsync("Request body is required.");
            return badReq;
        }

        VideoIngestionRequest? ingestionRequest;
        try
        {
            ingestionRequest = JsonSerializer.Deserialize<VideoIngestionRequest>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch
        {
            var badReq = req.CreateResponse(HttpStatusCode.BadRequest);
            await badReq.WriteStringAsync("Invalid JSON payload.");
            return badReq;
        }

        if (ingestionRequest is null || string.IsNullOrWhiteSpace(ingestionRequest.VideoUrl))
        {
            var badReq = req.CreateResponse(HttpStatusCode.BadRequest);
            await badReq.WriteStringAsync("VideoUrl is required.");
            return badReq;
        }

        var accountId = Environment.GetEnvironmentVariable("AzureVideoIndexer__AccountId") ?? "";
        var location = Environment.GetEnvironmentVariable("AzureVideoIndexer__Location") ?? "trial";
        var subscriptionKey = Environment.GetEnvironmentVariable("AzureVideoIndexer__SubscriptionKey") ?? "";

        try
        {
            var insights = await SubmitVideoAsync(ingestionRequest, accountId, location, subscriptionKey, executionContext.CancellationToken);
            var ok = req.CreateResponse(HttpStatusCode.OK);
            ok.Headers.Add("Content-Type", "application/json");
            await ok.WriteStringAsync(JsonSerializer.Serialize(insights));
            return ok;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ingest video {VideoUrl}", ingestionRequest.VideoUrl);
            var error = req.CreateResponse(HttpStatusCode.InternalServerError);
            await error.WriteStringAsync(ex.Message);
            return error;
        }
    }

    private async Task<VideoInsights> SubmitVideoAsync(
        VideoIngestionRequest request,
        string accountId,
        string location,
        string subscriptionKey,
        CancellationToken ct)
    {
        var accessToken = await GetAccessTokenAsync(accountId, location, subscriptionKey, ct);

        var uploadUrl = $"{ApiBaseUrl}/{location}/Accounts/{accountId}/Videos" +
                        $"?accessToken={Uri.EscapeDataString(accessToken)}" +
                        $"&name={Uri.EscapeDataString(string.IsNullOrEmpty(request.Name) ? "TurkAI-Video" : request.Name)}" +
                        $"&videoUrl={Uri.EscapeDataString(request.VideoUrl)}" +
                        $"&language={MapLanguage(request.Language)}" +
                        "&privacy=Private&indexingPreset=Default";

        var response = await _httpClient.PostAsync(uploadUrl, null, ct);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(body);
        var videoId = doc.RootElement.GetProperty("id").GetString()
                      ?? throw new InvalidOperationException("No video ID returned.");

        _logger.LogInformation("Video {VideoId} queued for indexing", videoId);

        return new VideoInsights
        {
            VideoId = videoId,
            Summary = "Video submitted. Processing in background — poll /video/{videoId}/insights.",
            Scenes = [],
            Keywords = [],
            Destinations = [],
            Transcript = null
        };
    }

    private async Task<string> GetAccessTokenAsync(string accountId, string location, string subscriptionKey, CancellationToken ct)
    {
        var tokenRequest = new HttpRequestMessage(
            HttpMethod.Get,
            $"{ApiBaseUrl}/auth/{location}/Accounts/{accountId}/AccessToken?allowEdit=false");
        tokenRequest.Headers.Add("Ocp-Apim-Subscription-Key", subscriptionKey);

        var response = await _httpClient.SendAsync(tokenRequest, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadAsStringAsync(ct)).Trim('"');
    }

    private static string MapLanguage(string lang) => lang switch
    {
        "tr" => "Turkish",
        "en" => "English",
        _ => "Auto"
    };
}
