using Microsoft.AspNetCore.Mvc;
using TurkAI.API.Services;
using TurkAI.Shared.Models;

namespace TurkAI.API.Controllers;

/// <summary>Personalisation endpoints — ML-based destination recommendations using Azure Personalizer.</summary>
[ApiController]
[Route("api/[controller]")]
public sealed class PersonalisationController : ControllerBase
{
    private readonly IPersonalisationService _personalisation;

    public PersonalisationController(IPersonalisationService personalisation) => _personalisation = personalisation;

    /// <summary>Get personalised destination recommendations for a user.</summary>
    [HttpPost("recommendations")]
    public async Task<ActionResult<PersonalisedRecommendation>> RecommendAsync(
        [FromBody] PersonalisationContext context,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(context.UserId))
            return BadRequest("UserId cannot be empty.");

        var result = await _personalisation.GetRecommendationsAsync(context, cancellationToken);
        return Ok(result);
    }

    /// <summary>Record user feedback on a destination to improve future recommendations.</summary>
    [HttpPost("feedback")]
    public async Task<IActionResult> FeedbackAsync(
        [FromQuery] string userId,
        [FromQuery] string destination,
        [FromQuery] double reward,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return BadRequest("userId cannot be empty.");

        await _personalisation.RecordFeedbackAsync(userId, destination, reward, cancellationToken);
        return NoContent();
    }
}
