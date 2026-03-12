using TurkAI.Shared.Models;

namespace TurkAI.API.Services;

public interface IVideoIndexerService
{
    Task<VideoInsights> IngestVideoAsync(VideoIngestionRequest request, CancellationToken cancellationToken = default);
    Task<VideoInsights?> GetVideoInsightsAsync(string videoId, CancellationToken cancellationToken = default);
}
