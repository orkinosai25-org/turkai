using TurkAI.Shared.Models;

namespace TurkAI.API.Services;

public interface ISpeechService
{
    /// <summary>Synthesise text to speech and return audio bytes (WAV).</summary>
    Task<byte[]> SynthesiseSpeechAsync(SpeechRequest request, CancellationToken cancellationToken = default);

    /// <summary>Transcribe audio bytes to text.</summary>
    Task<string> TranscribeAsync(byte[] audioBytes, string language = "tr-TR", CancellationToken cancellationToken = default);
}
