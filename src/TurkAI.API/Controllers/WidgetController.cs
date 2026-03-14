using Microsoft.AspNetCore.Mvc;

namespace TurkAI.API.Controllers;

/// <summary>Widget configuration endpoint — returns embed configuration for Enterprise clients.</summary>
[ApiController]
[Route("api/[controller]")]
public sealed class WidgetController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public WidgetController(IConfiguration configuration) => _configuration = configuration;

    /// <summary>Returns the public widget embed snippet for Enterprise clients.</summary>
    [HttpGet("embed")]
    public IActionResult GetEmbedSnippet([FromQuery] string apiKey, [FromQuery] string language = "en", [FromQuery] string theme = "light", [FromQuery] string position = "bottom-right")
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            return BadRequest(new { error = "apiKey query parameter is required." });

        var allowedLanguages = new[] { "en", "tr" };
        var allowedThemes = new[] { "light", "dark" };
        var allowedPositions = new[] { "bottom-right", "bottom-left" };

        if (!allowedLanguages.Contains(language)) language = "en";
        if (!allowedThemes.Contains(theme)) theme = "light";
        if (!allowedPositions.Contains(position)) position = "bottom-right";

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var apiBase = _configuration["ApiBaseUrl"] ?? baseUrl;
        var snippet = $"""
            <!-- TürkiyeAI Chat Widget -->
            <script src="{baseUrl}/widget.js"
                    data-api-key="{apiKey}"
                    data-language="{language}"
                    data-theme="{theme}"
                    data-position="{position}"
                    data-api-base="{apiBase}">
            </script>
            """;

        return Ok(new { snippet, widgetUrl = $"{baseUrl}/widget.js" });
    }
}
