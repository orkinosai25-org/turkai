using Microsoft.AspNetCore.Mvc;
using TurkAI.API.Services;
using TurkAI.Shared.Models;

namespace TurkAI.API.Controllers;

/// <summary>Video Indexer endpoints — ingest a travel video URL and retrieve extracted insights.</summary>
[ApiController]
[Route("api/[controller]")]
public sealed class VideoController : ControllerBase
{
    private readonly IVideoIndexerService _videoIndexer;

    public VideoController(IVideoIndexerService videoIndexer) => _videoIndexer = videoIndexer;

    /// <summary>Submit a video URL for ingestion and initial insight extraction.</summary>
    [HttpPost("ingest")]
    public async Task<ActionResult<VideoInsights>> IngestAsync(
        [FromBody] VideoIngestionRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.VideoUrl))
            return BadRequest("VideoUrl cannot be empty.");

        var result = await _videoIndexer.IngestVideoAsync(request, cancellationToken);
        return Ok(result);
    }

    /// <summary>Retrieve processed insights for a previously submitted video.</summary>
    [HttpGet("{videoId}/insights")]
    public async Task<ActionResult<VideoInsights>> GetInsightsAsync(
        string videoId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(videoId))
            return BadRequest("videoId cannot be empty.");

        var insights = await _videoIndexer.GetVideoInsightsAsync(videoId, cancellationToken);
        if (insights is null)
            return NotFound(new { message = "Video not found or not yet processed." });

        return Ok(insights);
    }
}
