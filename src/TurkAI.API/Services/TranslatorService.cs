using Azure;
using Azure.AI.Translation.Text;
using TurkAI.Shared.Models;

namespace TurkAI.API.Services;

/// <summary>Azure AI Translator integration (Turkish ↔ English and language detection).</summary>
public sealed class TranslatorService : ITranslatorService
{
    private readonly TextTranslationClient _client;
    private readonly ILogger<TranslatorService> _logger;

    public TranslatorService(IConfiguration configuration, ILogger<TranslatorService> logger)
    {
        var key = configuration["AzureTranslator:Key"]
            ?? throw new InvalidOperationException("AzureTranslator:Key is not configured.");
        var region = configuration["AzureTranslator:Region"]
            ?? throw new InvalidOperationException("AzureTranslator:Region is not configured.");

        _client = new TextTranslationClient(new AzureKeyCredential(key), region);
        _logger = logger;
    }

    public async Task<string> TranslateAsync(TranslationRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Translating to {Target}", request.TargetLanguage);

        var response = await _client.TranslateAsync(
            targetLanguage: request.TargetLanguage,
            text: request.Text,
            sourceLanguage: request.SourceLanguage,
            cancellationToken: cancellationToken);

        return response.Value.FirstOrDefault()?.Translations.FirstOrDefault()?.Text ?? request.Text;
    }

    public async Task<string> DetectLanguageAsync(string text, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Detecting language for text");

        // Translate with auto-detection — the response includes DetectedLanguage
        var response = await _client.TranslateAsync(
            targetLanguage: "en",
            text: text,
            sourceLanguage: null,
            cancellationToken: cancellationToken);

        return response.Value.FirstOrDefault()?.DetectedLanguage?.Language ?? "unknown";
    }
}

