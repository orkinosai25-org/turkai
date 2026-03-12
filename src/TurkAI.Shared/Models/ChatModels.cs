namespace TurkAI.Shared.Models;

/// <summary>Represents a single chat message in the conversation.</summary>
public record ChatMessage(string Role, string Content, DateTimeOffset Timestamp);

/// <summary>Request payload for the chat endpoint.</summary>
public class ChatRequest
{
    public required string Message { get; init; }
    public string Language { get; init; } = "en";
    public string? SessionId { get; init; }
    public List<ChatMessage> History { get; init; } = [];
}

/// <summary>Response from the chat endpoint.</summary>
public class ChatResponse
{
    public required string Reply { get; init; }
    public string Language { get; init; } = "en";
    public string? SessionId { get; init; }
    public List<ToolCall> ToolCalls { get; init; } = [];
}

/// <summary>Represents an AI tool call made during chat processing.</summary>
public class ToolCall
{
    public required string Name { get; init; }
    public required string Arguments { get; init; }
    public string? Result { get; init; }
}
