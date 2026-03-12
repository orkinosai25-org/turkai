using TurkAI.Shared.Models;

namespace TurkAI.API.Services;

public interface ILanguageService
{
    /// <summary>Extract key phrases from travel text.</summary>
    Task<IReadOnlyList<string>> ExtractKeyPhrasesAsync(string text, string language = "en", CancellationToken cancellationToken = default);

    /// <summary>Analyse sentiment of travel reviews or messages.</summary>
    Task<string> AnalyseSentimentAsync(string text, string language = "en", CancellationToken cancellationToken = default);

    /// <summary>Recognise named entities (locations, attractions, events).</summary>
    Task<IReadOnlyList<string>> RecogniseEntitiesAsync(string text, string language = "en", CancellationToken cancellationToken = default);
}
