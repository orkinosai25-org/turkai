namespace TurkAI.Shared.Models;

/// <summary>A travel query submitted by the user or a travel agent.</summary>
public class TravelQuery
{
    public required string Query { get; init; }
    public string Language { get; init; } = "en";
    public string? Destination { get; init; }
    public DateOnly? DepartureDate { get; init; }
    public DateOnly? ReturnDate { get; init; }
    public int Guests { get; init; } = 1;
    public List<string> Interests { get; init; } = [];
}

/// <summary>Structured travel information returned by the AI.</summary>
public class TravelInfo
{
    public required string Destination { get; init; }
    public required string Summary { get; init; }
    public List<string> Highlights { get; init; } = [];
    public List<string> PracticalTips { get; init; } = [];
    public string? BestTimeToVisit { get; init; }
    public string? Currency { get; init; }
    public string? Language { get; init; }
}

/// <summary>Request for image analysis of a travel-related image.</summary>
public class ImageAnalysisRequest
{
    public required string ImageUrl { get; init; }
    public string Language { get; init; } = "en";
}

/// <summary>Result of analysing a travel image.</summary>
public class ImageAnalysisResult
{
    public required string Description { get; init; }
    public List<string> Tags { get; init; } = [];
    public List<string> Landmarks { get; init; } = [];
    public string? Location { get; init; }
}

/// <summary>Request to ingest and analyse a video URL.</summary>
public class VideoIngestionRequest
{
    public required string VideoUrl { get; init; }
    public string Language { get; init; } = "en";
    public string Name { get; init; } = string.Empty;
}

/// <summary>Insights extracted from a travel video.</summary>
public class VideoInsights
{
    public required string VideoId { get; init; }
    public required string Summary { get; init; }
    public List<string> Scenes { get; init; } = [];
    public List<string> Keywords { get; init; } = [];
    public List<string> Destinations { get; init; } = [];
    public string? Transcript { get; init; }
}

/// <summary>Translation request (Turkish ↔ English).</summary>
public class TranslationRequest
{
    public required string Text { get; init; }
    public required string TargetLanguage { get; init; }
    public string? SourceLanguage { get; init; }
}

/// <summary>Speech synthesis request.</summary>
public class SpeechRequest
{
    public required string Text { get; init; }
    public string Language { get; init; } = "tr-TR";
    public string Voice { get; init; } = "tr-TR-AhmetNeural";
}

/// <summary>Personalisation context for a travel agent or end user.</summary>
public class PersonalisationContext
{
    public required string UserId { get; init; }
    public List<string> PreviousDestinations { get; init; } = [];
    public List<string> Preferences { get; init; } = [];
    public string? BudgetLevel { get; init; }
}

/// <summary>Personalised recommendation result.</summary>
public class PersonalisedRecommendation
{
    public required string UserId { get; init; }
    public List<string> RecommendedDestinations { get; init; } = [];
    public List<string> SuggestedActivities { get; init; } = [];
    public double ConfidenceScore { get; init; }
}
