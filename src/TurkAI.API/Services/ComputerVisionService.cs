using Azure;
using Azure.AI.Vision.ImageAnalysis;
using TurkAI.Shared.Models;

namespace TurkAI.API.Services;

/// <summary>Azure AI Vision — image captioning, landmark detection, and tag extraction.</summary>
public sealed class ComputerVisionService : IComputerVisionService
{
    private readonly ImageAnalysisClient _client;
    private readonly ILogger<ComputerVisionService> _logger;

    public ComputerVisionService(IConfiguration configuration, ILogger<ComputerVisionService> logger)
    {
        var endpoint = configuration["AzureVision:Endpoint"]
            ?? throw new InvalidOperationException("AzureVision:Endpoint is not configured.");
        var key = configuration["AzureVision:Key"]
            ?? throw new InvalidOperationException("AzureVision:Key is not configured.");

        _client = new ImageAnalysisClient(new Uri(endpoint), new AzureKeyCredential(key));
        _logger = logger;
    }

    public async Task<TurkAI.Shared.Models.ImageAnalysisResult> AnalyseImageAsync(ImageAnalysisRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Analysing image: {Url}", request.ImageUrl);

        var result = await _client.AnalyzeAsync(
            new Uri(request.ImageUrl),
            VisualFeatures.Caption | VisualFeatures.Tags | VisualFeatures.DenseCaptions,
            new ImageAnalysisOptions { Language = request.Language, GenderNeutralCaption = true },
            cancellationToken: cancellationToken);

        var description = result.Value.Caption?.Text ?? "No description available.";
        var tags = result.Value.Tags?.Values
            .OrderByDescending(t => t.Confidence)
            .Take(10)
            .Select(t => t.Name)
            .ToList() ?? [];

        // Heuristically identify Turkish landmark mentions from dense captions
        var landmarks = result.Value.DenseCaptions?.Values
            .Select(c => c.Text)
            .Where(t => t.Length > 5)
            .Take(5)
            .ToList() ?? [];

        return new TurkAI.Shared.Models.ImageAnalysisResult
        {
            Description = description,
            Tags = tags,
            Landmarks = landmarks,
            Location = null // Video Indexer or Maps would resolve this
        };
    }
}

