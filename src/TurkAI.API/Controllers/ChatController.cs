using Microsoft.AspNetCore.Mvc;
using TurkAI.API.Services;
using TurkAI.Shared.Models;

namespace TurkAI.API.Controllers;

/// <summary>Chat endpoint — processes messages via GPT-4o with the AI agent tool pipeline.</summary>
[ApiController]
[Route("api/[controller]")]
public sealed class ChatController : ControllerBase
{
    private readonly IAzureOpenAIService _openAI;
    private readonly ILogger<ChatController> _logger;

    public ChatController(IAzureOpenAIService openAI, ILogger<ChatController> logger)
    {
        _openAI = openAI;
        _logger = logger;
    }

    /// <summary>Send a message to TürkAI and receive a reply with optional tool-call metadata.</summary>
    [HttpPost]
    public async Task<ActionResult<ChatResponse>> PostAsync(
        [FromBody] ChatRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
            return BadRequest("Message cannot be empty.");

        _logger.LogInformation("Chat request received (lang={Language})", request.Language);

        var response = await _openAI.ChatAsync(request, cancellationToken);
        return Ok(response);
    }
}
