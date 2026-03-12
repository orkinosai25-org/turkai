using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;
using Azure.AI.OpenAI;
using OpenAI.Chat;
using TurkAI.Shared.Models;

namespace TurkAI.Functions.Functions;

/// <summary>
/// Azure Function that handles incoming chat requests from the queue or HTTP trigger,
/// runs the GPT-4o function-calling pipeline with the four TürkAI agent tools,
/// and returns a structured ChatResponse.
/// </summary>
public sealed class ChatOrchestrator
{
    private readonly AzureOpenAIClient _openAIClient;
    private readonly ILogger<ChatOrchestrator> _logger;

    public ChatOrchestrator(AzureOpenAIClient openAIClient, ILogger<ChatOrchestrator> logger)
    {
        _openAIClient = openAIClient;
        _logger = logger;
    }

    /// <summary>HTTP trigger — direct chat endpoint for low-latency use cases.</summary>
    [Function("Chat")]
    public async Task<HttpResponseData> RunHttpAsync(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "chat")] HttpRequestData req,
        FunctionContext executionContext)
    {
        _logger.LogInformation("Chat HTTP trigger fired");

        var body = await req.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(body))
        {
            var badReq = req.CreateResponse(HttpStatusCode.BadRequest);
            await badReq.WriteStringAsync("Request body is required.");
            return badReq;
        }

        ChatRequest? chatRequest;
        try
        {
            chatRequest = JsonSerializer.Deserialize<ChatRequest>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch
        {
            var badReq = req.CreateResponse(HttpStatusCode.BadRequest);
            await badReq.WriteStringAsync("Invalid JSON payload.");
            return badReq;
        }

        if (chatRequest is null || string.IsNullOrWhiteSpace(chatRequest.Message))
        {
            var badReq = req.CreateResponse(HttpStatusCode.BadRequest);
            await badReq.WriteStringAsync("Message field is required.");
            return badReq;
        }

        var chatResponse = await ProcessChatAsync(chatRequest, executionContext.CancellationToken);

        var ok = req.CreateResponse(HttpStatusCode.OK);
        ok.Headers.Add("Content-Type", "application/json");
        await ok.WriteStringAsync(JsonSerializer.Serialize(chatResponse));
        return ok;
    }

    /// <summary>Queue trigger — process chat jobs asynchronously from a Storage Queue.</summary>
    [Function("ChatQueue")]
    public async Task RunQueueAsync(
        [QueueTrigger("turkai-chat-queue", Connection = "AzureWebJobsStorage")] string queueMessage,
        FunctionContext executionContext)
    {
        _logger.LogInformation("Chat queue trigger fired");

        var chatRequest = JsonSerializer.Deserialize<ChatRequest>(queueMessage, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (chatRequest is null) return;

        await ProcessChatAsync(chatRequest, executionContext.CancellationToken);
    }

    private async Task<ChatResponse> ProcessChatAsync(ChatRequest request, CancellationToken ct)
    {
        var deploymentName = Environment.GetEnvironmentVariable("AzureOpenAI__DeploymentName") ?? "gpt-4o";
        var chatClient = _openAIClient.GetChatClient(deploymentName);

        var messages = new List<OpenAI.Chat.ChatMessage>
        {
            new SystemChatMessage("You are TürkAI, an expert AI travel assistant for Türkiye. You speak English and Turkish.")
        };

        foreach (var h in request.History)
        {
            if (h.Role == "user") messages.Add(new UserChatMessage(h.Content));
            else if (h.Role == "assistant") messages.Add(new AssistantChatMessage(h.Content));
        }
        messages.Add(new UserChatMessage(request.Message));

        var options = new ChatCompletionOptions { MaxOutputTokenCount = 2048 };
        foreach (var tool in AiToolDefinitions.All) options.Tools.Add(tool);

        var toolCallsLog = new List<ToolCall>();
        string finalReply;

        while (true)
        {
            var completion = await chatClient.CompleteChatAsync(messages, options, ct);
            var choice = completion.Value;

            if (choice.FinishReason == ChatFinishReason.ToolCalls)
            {
                messages.Add(new AssistantChatMessage(choice));
                foreach (var toolCall in choice.ToolCalls)
                {
                    var result = $"Tool {toolCall.FunctionName} called with: {toolCall.FunctionArguments}";
                    toolCallsLog.Add(new ToolCall { Name = toolCall.FunctionName, Arguments = toolCall.FunctionArguments.ToString(), Result = result });
                    messages.Add(new ToolChatMessage(toolCall.Id, result));
                }
                continue;
            }

            finalReply = choice.Content[0].Text;
            break;
        }

        return new ChatResponse
        {
            Reply = finalReply,
            Language = request.Language,
            SessionId = request.SessionId ?? Guid.NewGuid().ToString(),
            ToolCalls = toolCallsLog
        };
    }
}
