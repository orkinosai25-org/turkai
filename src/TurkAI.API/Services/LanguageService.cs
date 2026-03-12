using Azure;
using Azure.AI.Language.Text;
using TurkAI.Shared.Models;

namespace TurkAI.API.Services;

/// <summary>Azure AI Language integration — key phrases, sentiment, and NER.</summary>
public sealed class LanguageService : ILanguageService
{
    private readonly TextAnalysisClient _client;
    private readonly ILogger<LanguageService> _logger;

    public LanguageService(IConfiguration configuration, ILogger<LanguageService> logger)
    {
        var endpoint = configuration["AzureLanguage:Endpoint"]
            ?? throw new InvalidOperationException("AzureLanguage:Endpoint is not configured.");
        var key = configuration["AzureLanguage:Key"]
            ?? throw new InvalidOperationException("AzureLanguage:Key is not configured.");

        _client = new TextAnalysisClient(new Uri(endpoint), new AzureKeyCredential(key));
        _logger = logger;
    }

    public async Task<IReadOnlyList<string>> ExtractKeyPhrasesAsync(string text, string language = "en", CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Extracting key phrases from text (lang={Language})", language);

        var input = new TextKeyPhraseExtractionInput
        {
            TextInput = new MultiLanguageTextInput
            {
                MultiLanguageInputs = { new MultiLanguageInput("1", text) { Language = language } }
            }
        };

        var response = await _client.AnalyzeTextAsync(input, cancellationToken: cancellationToken);

        if (response.Value is AnalyzeTextKeyPhraseResult kpResult)
        {
            return kpResult.Results.Documents
                .SelectMany(d => d.KeyPhrases)
                .ToList();
        }

        return [];
    }

    public async Task<string> AnalyseSentimentAsync(string text, string language = "en", CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Analysing sentiment (lang={Language})", language);

        var input = new TextSentimentAnalysisInput
        {
            TextInput = new MultiLanguageTextInput
            {
                MultiLanguageInputs = { new MultiLanguageInput("1", text) { Language = language } }
            }
        };

        var response = await _client.AnalyzeTextAsync(input, cancellationToken: cancellationToken);

        if (response.Value is AnalyzeTextSentimentResult sentimentResult)
        {
            var doc = sentimentResult.Results.Documents.FirstOrDefault();
            return doc?.Sentiment.ToString() ?? "unknown";
        }

        return "unknown";
    }

    public async Task<IReadOnlyList<string>> RecogniseEntitiesAsync(string text, string language = "en", CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Recognising entities (lang={Language})", language);

        var input = new TextEntityRecognitionInput
        {
            TextInput = new MultiLanguageTextInput
            {
                MultiLanguageInputs = { new MultiLanguageInput("1", text) { Language = language } }
            }
        };

        var response = await _client.AnalyzeTextAsync(input, cancellationToken: cancellationToken);

        if (response.Value is AnalyzeTextEntitiesResult nerResult)
        {
            return nerResult.Results.Documents
                .SelectMany(d => d.Entities)
                .Select(e => $"{e.Text} ({e.Category})")
                .ToList();
        }

        return [];
    }
}

