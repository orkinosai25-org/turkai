using System.Text.Json;
using Azure.AI.OpenAI;
using OpenAI.Chat;
using TurkAI.Shared.Models;

namespace TurkAI.API.Services;

/// <summary>
/// Integrates with Azure OpenAI (GPT-4o) and exposes the four AI agent tools
/// via the function-calling pipeline.
/// </summary>
public sealed class AzureOpenAIService : IAzureOpenAIService
{
    private readonly ChatClient _chatClient;
    private readonly ILogger<AzureOpenAIService> _logger;
    private readonly string _deploymentName;

    // Injected tool implementations
    private readonly ITranslatorService _translator;
    private readonly IComputerVisionService _vision;
    private readonly IVideoIndexerService _videoIndexer;

    private static readonly string TurkAISystemPrompt = """
        You are TürkAI, an expert AI travel assistant specialising exclusively in Türkiye (Turkey).
        You speak both Turkish and English fluently and seamlessly switch between languages based on the user's preference.
        You have deep knowledge of Turkish history, culture, cuisine, geography, transport, visa requirements, and hospitality.

        You power the TürkiyeAI booking platform (turkiyeai.travel) which lets travellers and travel agents
        search and book flights, hotels, resorts, airport transfers, excursions, car hire, yacht charters,
        cruises, and private aviation — all in Türkiye.

        When asked about destinations, always use the `get_travel_info` tool to provide accurate, structured information.
        When asked about hotels or resorts, use the `get_hotel_recommendations` tool.
        When asked about activities, tours, excursions, transfers, car hire, cruises, yachts, or packages,
        use the `get_travel_services` tool — it covers all travel service verticals.
        When you encounter non-English / non-Turkish text, use `translate_content` to understand and respond appropriately.
        When an image URL is shared, use `analyse_image` to describe it in context.
        When a video URL is shared, use `get_video_insights` to extract travel insights.

        Always be friendly, professional, and helpful.
        Tailor responses for both individual travellers and travel agents / hospitality businesses.
        When surfacing service or booking options, remind users that actual bookings are completed
        via licensed providers on the TürkiyeAI platform.
        """;

    public AzureOpenAIService(
        AzureOpenAIClient openAIClient,
        IConfiguration configuration,
        ITranslatorService translator,
        IComputerVisionService vision,
        IVideoIndexerService videoIndexer,
        ILogger<AzureOpenAIService> logger)
    {
        _deploymentName = configuration["AzureOpenAI:DeploymentName"]
            ?? throw new InvalidOperationException("AzureOpenAI:DeploymentName is not configured.");
        _chatClient = openAIClient.GetChatClient(_deploymentName);
        _translator = translator;
        _vision = vision;
        _videoIndexer = videoIndexer;
        _logger = logger;
    }

    public async Task<ChatResponse> ChatAsync(ChatRequest request, CancellationToken cancellationToken = default)
    {
        var messages = new List<OpenAI.Chat.ChatMessage>
        {
            new SystemChatMessage(TurkAISystemPrompt)
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
            var completion = await _chatClient.CompleteChatAsync(messages, options, cancellationToken);
            var choice = completion.Value;

            if (choice.FinishReason == ChatFinishReason.ToolCalls)
            {
                messages.Add(new AssistantChatMessage(choice));

                var toolResultMessages = new List<ToolChatMessage>();
                foreach (var toolCall in choice.ToolCalls)
                {
                    var result = await DispatchToolCallAsync(toolCall, cancellationToken);
                    toolCallsLog.Add(new ToolCall
                    {
                        Name = toolCall.FunctionName,
                        Arguments = toolCall.FunctionArguments.ToString(),
                        Result = result
                    });
                    toolResultMessages.Add(new ToolChatMessage(toolCall.Id, result));
                }
                messages.AddRange(toolResultMessages);
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

    public async Task<string> CompleteAsync(string systemPrompt, string userMessage, CancellationToken cancellationToken = default)
    {
        var messages = new List<OpenAI.Chat.ChatMessage>
        {
            new SystemChatMessage(systemPrompt),
            new UserChatMessage(userMessage)
        };

        var completion = await _chatClient.CompleteChatAsync(messages, cancellationToken: cancellationToken);
        return completion.Value.Content[0].Text;
    }

    private async Task<string> DispatchToolCallAsync(ChatToolCall toolCall, CancellationToken ct)
    {
        try
        {
            return toolCall.FunctionName switch
            {
                "get_travel_info" => await HandleGetTravelInfoAsync(toolCall.FunctionArguments.ToString(), ct),
                "get_hotel_recommendations" => await HandleGetHotelRecommendationsAsync(toolCall.FunctionArguments.ToString(), ct),
                "get_travel_services" => await HandleGetTravelServicesAsync(toolCall.FunctionArguments.ToString(), ct),
                "translate_content" => await HandleTranslateContentAsync(toolCall.FunctionArguments.ToString(), ct),
                "analyse_image" => await HandleAnalyseImageAsync(toolCall.FunctionArguments.ToString(), ct),
                "get_video_insights" => await HandleGetVideoInsightsAsync(toolCall.FunctionArguments.ToString(), ct),
                _ => $"Unknown tool: {toolCall.FunctionName}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Tool call {ToolName} failed", toolCall.FunctionName);
            return $"Error executing {toolCall.FunctionName}: {ex.Message}";
        }
    }

    private async Task<string> HandleGetTravelInfoAsync(string arguments, CancellationToken ct)
    {
        using var doc = JsonDocument.Parse(arguments);
        var destination = doc.RootElement.GetProperty("destination").GetString() ?? "";
        var language = doc.RootElement.TryGetProperty("language", out var lang) ? lang.GetString() ?? "en" : "en";

        // Use GPT-4o itself to produce structured travel info (no external DB needed at this stage)
        var prompt = $"Provide structured travel information for {destination} in Türkiye in {(language == "tr" ? "Turkish" : "English")}. " +
                     "Include: summary (2 sentences), top 5 highlights, 3 practical tips, best time to visit, currency, and language notes. " +
                     "Respond as minified JSON with keys: destination, summary, highlights[], practicalTips[], bestTimeToVisit, currency, language.";

        var json = await CompleteAsync("You are a JSON-only travel data API. Return only valid minified JSON.", prompt, ct);
        return json;
    }

    private async Task<string> HandleGetHotelRecommendationsAsync(string arguments, CancellationToken ct)
    {
        using var doc = JsonDocument.Parse(arguments);
        var destination = doc.RootElement.GetProperty("destination").GetString() ?? "";
        var category = doc.RootElement.TryGetProperty("category", out var cat) ? cat.GetString() ?? "all" : "all";
        var language = doc.RootElement.TryGetProperty("language", out var lang) ? lang.GetString() ?? "en" : "en";
        var interests = doc.RootElement.TryGetProperty("interests", out var intEl)
            ? intEl.EnumerateArray().Select(e => e.GetString() ?? "").ToList()
            : [];

        var interestClause = interests.Count > 0 ? $" for travellers interested in {string.Join(", ", interests)}" : "";
        var categoryClause = category != "all" ? $" Focus on {category} options." : "";
        var responseLang = language == "tr" ? "Turkish" : "English";

        var prompt = $"""
            Recommend 5 real hotels or resorts near {destination} in Türkiye{interestClause}.{categoryClause}
            For each, include: name, category (luxury/boutique/mid-range/budget), a 2-sentence description,
            top 3 nearby attractions within 30 minutes, key amenities, and approximate price range per night in EUR.
            Respond in {responseLang} as minified JSON with key: recommendations (array of objects with keys:
            name, category, description, nearbyAttractions[], amenities[], priceRangeEur).
            """;

        var json = await CompleteAsync("You are a JSON-only hotel recommendation API for Türkiye. Return only valid minified JSON.", prompt, ct);
        return json;
    }

    private async Task<string> HandleGetTravelServicesAsync(string arguments, CancellationToken ct)
    {
        using var doc = JsonDocument.Parse(arguments);
        var destination = doc.RootElement.GetProperty("destination").GetString() ?? "";
        var language = doc.RootElement.TryGetProperty("language", out var lang) ? lang.GetString() ?? "en" : "en";
        var budgetLevel = doc.RootElement.TryGetProperty("budget_level", out var bud) ? bud.GetString() ?? "" : "";
        var arrivalAirport = doc.RootElement.TryGetProperty("arrival_airport", out var apt) ? apt.GetString() ?? "" : "";

        var serviceTypes = new List<string> { "all" };
        if (doc.RootElement.TryGetProperty("service_types", out var stEl))
        {
            serviceTypes = stEl.EnumerateArray().Select(e => e.GetString() ?? "").ToList();
        }

        var includeAll = serviceTypes.Contains("all");
        var budgetClause = !string.IsNullOrEmpty(budgetLevel) ? $" Tailor suggestions for a {budgetLevel} budget." : "";
        var airportClause = !string.IsNullOrEmpty(arrivalAirport) ? $" The traveller arrives at {arrivalAirport} airport." : "";
        var responseLang = language == "tr" ? "Turkish" : "English";

        var sections = new List<string>();
        if (includeAll || serviceTypes.Contains("excursions"))
            sections.Add("excursions: 3 top day-trips or experiences (title, type, duration, approx price EUR per person, highlights)");
        if (includeAll || serviceTypes.Contains("transfers"))
            sections.Add("transfers: 2 airport transfer options (vehicle type, approx EUR price, duration estimate)");
        if (includeAll || serviceTypes.Contains("packages"))
            sections.Add("packages: 2 holiday packages (title, board_basis, duration_nights, from price EUR pp)");
        if (includeAll || serviceTypes.Contains("cars"))
            sections.Add("cars: 2 car hire categories (category, seats, approx EUR per day, supplier note)");
        if (includeAll || serviceTypes.Contains("cruises"))
            sections.Add("cruises: 1 relevant cruise or gulet itinerary (title, ship_type, duration_nights, departure_port, highlights)");
        if (includeAll || serviceTypes.Contains("yachts"))
            sections.Add("yachts: 1 yacht or gulet charter option (vessel_type, max_guests, approx EUR per week, home_port)");
        if (includeAll || serviceTypes.Contains("private-aviation"))
            sections.Add("private_aviation: 1 private jet option (aircraft_type, max_passengers, approx EUR per sector)");

        if (sections.Count == 0)
            sections.Add("excursions: 3 top day-trips or experiences");

        var prompt = $"""
            For the destination {destination} in Türkiye, recommend travel services for a visitor.{airportClause}{budgetClause}
            Include the following sections: {string.Join("; ", sections)}.
            Respond in {responseLang} as minified JSON.
            Top-level keys must match the section names (excursions, transfers, packages, cars, cruises, yachts, private_aviation).
            Each key is an array of objects with the fields listed for that section.
            """;

        var json = await CompleteAsync("You are a JSON-only travel services API for Türkiye. Return only valid minified JSON.", prompt, ct);
        return json;
    }

    private async Task<string> HandleTranslateContentAsync(string arguments, CancellationToken ct)
    {
        using var doc = JsonDocument.Parse(arguments);
        var text = doc.RootElement.GetProperty("text").GetString() ?? "";
        var target = doc.RootElement.GetProperty("target_language").GetString() ?? "en";
        var source = doc.RootElement.TryGetProperty("source_language", out var src) ? src.GetString() : null;

        var translated = await _translator.TranslateAsync(
            new TranslationRequest { Text = text, TargetLanguage = target, SourceLanguage = source }, ct);

        return JsonSerializer.Serialize(new { translated, target_language = target });
    }

    private async Task<string> HandleAnalyseImageAsync(string arguments, CancellationToken ct)
    {
        using var doc = JsonDocument.Parse(arguments);
        var imageUrl = doc.RootElement.GetProperty("image_url").GetString() ?? "";
        var language = doc.RootElement.TryGetProperty("language", out var lang) ? lang.GetString() ?? "en" : "en";

        var result = await _vision.AnalyseImageAsync(
            new ImageAnalysisRequest { ImageUrl = imageUrl, Language = language }, ct);

        return JsonSerializer.Serialize(result);
    }

    private async Task<string> HandleGetVideoInsightsAsync(string arguments, CancellationToken ct)
    {
        using var doc = JsonDocument.Parse(arguments);
        var videoUrl = doc.RootElement.GetProperty("video_url").GetString() ?? "";
        var language = doc.RootElement.TryGetProperty("language", out var lang) ? lang.GetString() ?? "en" : "en";
        var name = doc.RootElement.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";

        var insights = await _videoIndexer.IngestVideoAsync(
            new VideoIngestionRequest { VideoUrl = videoUrl, Language = language, Name = name }, ct);

        return JsonSerializer.Serialize(insights);
    }
}
