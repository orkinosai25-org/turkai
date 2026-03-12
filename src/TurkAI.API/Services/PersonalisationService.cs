using Azure;
using Azure.AI.Personalizer;
using TurkAI.Shared.Models;

namespace TurkAI.API.Services;

/// <summary>
/// Azure AI Personalizer integration — uses contextual bandits to produce
/// personalised Turkish destination recommendations based on user history.
/// </summary>
public sealed class PersonalisationService : IPersonalisationService
{
    private readonly PersonalizerClient _client;
    private readonly ILogger<PersonalisationService> _logger;

    // Known Turkish destinations that can be ranked
    private static readonly string[] TurkishDestinations =
    [
        "Istanbul", "Cappadocia", "Antalya", "Bodrum", "Ephesus",
        "Pamukkale", "Trabzon", "Ankara", "Izmir", "Konya",
        "Gallipoli", "Bursa", "Alanya", "Kas", "Fethiye"
    ];

    public PersonalisationService(IConfiguration configuration, ILogger<PersonalisationService> logger)
    {
        var endpoint = configuration["AzurePersonalizer:Endpoint"]
            ?? throw new InvalidOperationException("AzurePersonalizer:Endpoint is not configured.");
        var key = configuration["AzurePersonalizer:Key"]
            ?? throw new InvalidOperationException("AzurePersonalizer:Key is not configured.");

        _client = new PersonalizerClient(new Uri(endpoint), new AzureKeyCredential(key));
        _logger = logger;
    }

    public async Task<PersonalisedRecommendation> GetRecommendationsAsync(
        PersonalisationContext context,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting personalised recommendations for user {UserId}", context.UserId);

        var contextFeatures = new List<object>
        {
            new { userId = context.UserId },
            new { preferences = context.Preferences },
            new { budget = context.BudgetLevel ?? "medium" },
            new { previousDestinations = context.PreviousDestinations }
        };

        var actions = TurkishDestinations
            .Where(d => !context.PreviousDestinations.Contains(d))
            .Select((d, i) => new PersonalizerRankableAction(
                id: d,
                features: [new { name = d, type = "destination" }]))
            .ToList();

        if (actions.Count == 0)
        {
            // All destinations visited — recommend revisiting top-rated ones
            actions = TurkishDestinations
                .Take(5)
                .Select(d => new PersonalizerRankableAction(
                    id: d,
                    features: [new { name = d, type = "destination" }]))
                .ToList();
        }

        var rankResult = await _client.RankAsync(actions, contextFeatures, cancellationToken);

        var recommended = rankResult.Value.Ranking
            .OrderByDescending(r => r.Probability)
            .Take(5)
            .Select(r => r.Id)
            .ToList();

        return new PersonalisedRecommendation
        {
            UserId = context.UserId,
            RecommendedDestinations = recommended,
            SuggestedActivities = SuggestActivities(context.Preferences),
            ConfidenceScore = (double)(rankResult.Value.Ranking.Max(r => r.Probability) ?? 0f)
        };
    }

    public async Task RecordFeedbackAsync(string userId, string destination, double reward, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Recording feedback for user {UserId}, destination {Destination}, reward {Reward}",
            userId, destination, reward);

        await _client.RewardAsync(userId, (float)reward, cancellationToken);
    }

    private static List<string> SuggestActivities(List<string> preferences)
    {
        var suggestions = new List<string>();
        foreach (var pref in preferences)
        {
            suggestions.AddRange(pref.ToLowerInvariant() switch
            {
                "history" => ["Visit ancient ruins", "Explore Ottoman palaces", "Guided heritage tour"],
                "food" => ["Turkish street food tour", "Cooking class in Istanbul", "Spice bazaar visit"],
                "beaches" => ["Ölüdeniz Blue Lagoon", "Bodrum beach clubs", "Aegean sailing trip"],
                "adventure" => ["Hot-air balloon over Cappadocia", "Whitewater rafting in Köprülü Canyon", "Trekking Lycian Way"],
                "culture" => ["Whirling dervish ceremony", "Istanbul Grand Bazaar", "Traditional hammam"],
                _ => ["Explore local markets", "Turkish tea ceremony experience"]
            });
        }
        return suggestions.Distinct().Take(6).ToList();
    }
}
