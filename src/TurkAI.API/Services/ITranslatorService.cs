using TurkAI.Shared.Models;

namespace TurkAI.API.Services;

public interface ITranslatorService
{
    Task<string> TranslateAsync(TranslationRequest request, CancellationToken cancellationToken = default);
    Task<string> DetectLanguageAsync(string text, CancellationToken cancellationToken = default);
}
