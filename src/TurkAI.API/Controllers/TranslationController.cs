using Microsoft.AspNetCore.Mvc;
using TurkAI.API.Services;
using TurkAI.Shared.Models;

namespace TurkAI.API.Controllers;

/// <summary>Translation and language detection endpoints.</summary>
[ApiController]
[Route("api/[controller]")]
public sealed class TranslationController : ControllerBase
{
    private readonly ITranslatorService _translator;
    private readonly ILanguageService _language;

    public TranslationController(ITranslatorService translator, ILanguageService language)
    {
        _translator = translator;
        _language = language;
    }

    /// <summary>Translate text between Turkish and English.</summary>
    [HttpPost("translate")]
    public async Task<ActionResult<string>> TranslateAsync(
        [FromBody] TranslationRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
            return BadRequest("Text cannot be empty.");

        var result = await _translator.TranslateAsync(request, cancellationToken);
        return Ok(new { translated = result });
    }

    /// <summary>Detect the language of the provided text.</summary>
    [HttpPost("detect")]
    public async Task<ActionResult<string>> DetectAsync(
        [FromBody] TextInputRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
            return BadRequest("Text cannot be empty.");

        var lang = await _translator.DetectLanguageAsync(request.Text, cancellationToken);
        return Ok(new { language = lang });
    }

    /// <summary>Extract key phrases from travel text using Azure AI Language (NLP).</summary>
    [HttpPost("keyphrases")]
    public async Task<ActionResult<IReadOnlyList<string>>> KeyPhrasesAsync(
        [FromBody] TextInputRequest request,
        [FromQuery] string language = "en",
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
            return BadRequest("Text cannot be empty.");

        var phrases = await _language.ExtractKeyPhrasesAsync(request.Text, language, cancellationToken);
        return Ok(phrases);
    }

    /// <summary>Analyse sentiment of travel reviews or messages.</summary>
    [HttpPost("sentiment")]
    public async Task<ActionResult<string>> SentimentAsync(
        [FromBody] TextInputRequest request,
        [FromQuery] string language = "en",
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
            return BadRequest("Text cannot be empty.");

        var sentiment = await _language.AnalyseSentimentAsync(request.Text, language, cancellationToken);
        return Ok(new { sentiment });
    }
}

/// <summary>Simple text input wrapper used where a raw string would be needed as request body.</summary>
public record TextInputRequest(string Text);

