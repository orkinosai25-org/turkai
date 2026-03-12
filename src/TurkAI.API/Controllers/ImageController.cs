using Microsoft.AspNetCore.Mvc;
using TurkAI.API.Services;
using TurkAI.Shared.Models;

namespace TurkAI.API.Controllers;

/// <summary>Computer vision endpoint — analyse travel images for captions, tags, and landmarks.</summary>
[ApiController]
[Route("api/[controller]")]
public sealed class ImageController : ControllerBase
{
    private readonly IComputerVisionService _vision;

    public ImageController(IComputerVisionService vision) => _vision = vision;

    /// <summary>Analyse a publicly accessible travel image URL.</summary>
    [HttpPost("analyse")]
    public async Task<ActionResult<ImageAnalysisResult>> AnalyseAsync(
        [FromBody] ImageAnalysisRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ImageUrl))
            return BadRequest("ImageUrl cannot be empty.");

        var result = await _vision.AnalyseImageAsync(request, cancellationToken);
        return Ok(result);
    }
}
