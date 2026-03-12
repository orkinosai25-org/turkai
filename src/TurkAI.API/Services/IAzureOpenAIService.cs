using TurkAI.Shared.Models;

namespace TurkAI.API.Services;

public interface IAzureOpenAIService
{
    Task<ChatResponse> ChatAsync(ChatRequest request, CancellationToken cancellationToken = default);
    Task<string> CompleteAsync(string systemPrompt, string userMessage, CancellationToken cancellationToken = default);
}
