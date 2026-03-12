using Microsoft.AspNetCore.Mvc;
using TurkAI.API.Services;
using TurkAI.Shared.Models;

namespace TurkAI.API.Controllers;

/// <summary>Speech endpoints — text-to-speech synthesis and speech-to-text transcription.</summary>
[ApiController]
[Route("api/[controller]")]
public sealed class SpeechController : ControllerBase
{
    private readonly ISpeechService _speech;

    public SpeechController(ISpeechService speech) => _speech = speech;

    /// <summary>Synthesise text to speech and return the audio bytes (WAV, 16 kHz mono PCM).</summary>
    [HttpPost("synthesise")]
    public async Task<IActionResult> SynthesiseAsync(
        [FromBody] SpeechRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
            return BadRequest("Text cannot be empty.");

        var audio = await _speech.SynthesiseSpeechAsync(request, cancellationToken);
        return File(audio, "audio/wav");
    }

    /// <summary>Transcribe uploaded audio bytes to text.</summary>
    [HttpPost("transcribe")]
    public async Task<ActionResult<string>> TranscribeAsync(
        IFormFile audio,
        [FromQuery] string language = "tr-TR",
        CancellationToken cancellationToken = default)
    {
        if (audio is null || audio.Length == 0)
            return BadRequest("Audio file cannot be empty.");

        using var ms = new MemoryStream();
        await audio.CopyToAsync(ms, cancellationToken);

        var text = await _speech.TranscribeAsync(ms.ToArray(), language, cancellationToken);
        return Ok(new { transcript = text });
    }
}
