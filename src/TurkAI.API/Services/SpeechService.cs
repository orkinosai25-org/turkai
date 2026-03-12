using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using TurkAI.Shared.Models;

namespace TurkAI.API.Services;

/// <summary>Azure Cognitive Services Speech integration — synthesis and recognition.</summary>
public sealed class SpeechService : ISpeechService
{
    private readonly string _subscriptionKey;
    private readonly string _region;
    private readonly ILogger<SpeechService> _logger;

    public SpeechService(IConfiguration configuration, ILogger<SpeechService> logger)
    {
        _subscriptionKey = configuration["AzureSpeech:Key"]
            ?? throw new InvalidOperationException("AzureSpeech:Key is not configured.");
        _region = configuration["AzureSpeech:Region"]
            ?? throw new InvalidOperationException("AzureSpeech:Region is not configured.");
        _logger = logger;
    }

    public async Task<byte[]> SynthesiseSpeechAsync(SpeechRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Synthesising speech in {Language} with voice {Voice}", request.Language, request.Voice);

        var config = SpeechConfig.FromSubscription(_subscriptionKey, _region);
        config.SpeechSynthesisLanguage = request.Language;
        config.SpeechSynthesisVoiceName = request.Voice;
        config.SetSpeechSynthesisOutputFormat(SpeechSynthesisOutputFormat.Riff16Khz16BitMonoPcm);

        using var stream = AudioOutputStream.CreatePullStream();
        using var audioConfig = AudioConfig.FromStreamOutput(stream);
        using var synthesiser = new SpeechSynthesizer(config, audioConfig);

        var result = await synthesiser.SpeakTextAsync(request.Text);

        if (result.Reason == ResultReason.SynthesizingAudioCompleted)
        {
            return result.AudioData;
        }

        var cancellation = SpeechSynthesisCancellationDetails.FromResult(result);
        throw new InvalidOperationException($"Speech synthesis failed: {cancellation.ErrorDetails}");
    }

    public async Task<string> TranscribeAsync(byte[] audioBytes, string language = "tr-TR", CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Transcribing audio in {Language}", language);

        var config = SpeechConfig.FromSubscription(_subscriptionKey, _region);
        config.SpeechRecognitionLanguage = language;

        using var audioStream = AudioInputStream.CreatePushStream();
        audioStream.Write(audioBytes);
        audioStream.Close();

        using var audioConfig = AudioConfig.FromStreamInput(audioStream);
        using var recogniser = new SpeechRecognizer(config, audioConfig);

        var result = await recogniser.RecognizeOnceAsync();

        return result.Reason switch
        {
            ResultReason.RecognizedSpeech => result.Text,
            ResultReason.NoMatch => string.Empty,
            _ => throw new InvalidOperationException($"Speech recognition failed: {result.Reason}")
        };
    }
}
