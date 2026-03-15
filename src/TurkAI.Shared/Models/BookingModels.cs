namespace TurkAI.Shared.Models;

/// <summary>
/// Request for an AI-powered booking recommendation.
/// Submitted by the TürkiyeAI booking SaaS (partner tier) to get a structured
/// recommendation bundle covering accommodation, transfers, excursions, and more.
/// </summary>
public class BookingRecommendationRequest
{
    /// <summary>The traveller's natural-language query or intent.</summary>
    public required string Query { get; init; }

    /// <summary>Primary destination in Türkiye (e.g. "Bodrum", "Antalya").</summary>
    public required string Destination { get; init; }

    /// <summary>ISO date of arrival (YYYY-MM-DD).</summary>
    public string? ArrivalDate { get; init; }

    /// <summary>ISO date of departure (YYYY-MM-DD).</summary>
    public string? DepartureDate { get; init; }

    /// <summary>Number of adult guests.</summary>
    public int Adults { get; init; } = 2;

    /// <summary>Number of children.</summary>
    public int Children { get; init; }

    /// <summary>Preferred accommodation category.</summary>
    public string AccommodationCategory { get; init; } = "all";

    /// <summary>Traveller interests used to personalise recommendations.</summary>
    public List<string> Interests { get; init; } = [];

    /// <summary>Preferred language for the AI response ("en" or "tr").</summary>
    public string Language { get; init; } = "en";

    /// <summary>Optional originating airport IATA code for transfer suggestions (e.g. "AYT").</summary>
    public string? ArrivalAirport { get; init; }

    /// <summary>Budget level hint ("budget", "mid-range", "luxury").</summary>
    public string? BudgetLevel { get; init; }
}

/// <summary>
/// AI-powered booking recommendation bundle returned by the partner-only
/// <c>POST /api/booking/recommend</c> endpoint.
/// </summary>
public class BookingRecommendationResponse
{
    /// <summary>Destination the recommendation is for.</summary>
    public required string Destination { get; init; }

    /// <summary>Natural-language AI summary of the recommendation.</summary>
    public required string AiSummary { get; init; }

    /// <summary>Recommended accommodation options.</summary>
    public List<AccommodationRecommendation> Accommodations { get; init; } = [];

    /// <summary>Suggested excursions or experiences.</summary>
    public List<ServiceRecommendation> Excursions { get; init; } = [];

    /// <summary>Suggested airport transfer options.</summary>
    public List<ServiceRecommendation> Transfers { get; init; } = [];

    /// <summary>Other relevant service suggestions (car hire, yacht, etc.).</summary>
    public List<ServiceRecommendation> OtherServices { get; init; } = [];

    /// <summary>Pre-packaged holiday packages for the destination.</summary>
    public List<ServiceRecommendation> Packages { get; init; } = [];
}

/// <summary>A single accommodation recommendation within a booking bundle.</summary>
public class AccommodationRecommendation
{
    public required string Name { get; init; }
    public required string Category { get; init; }
    public required string Description { get; init; }
    public string? PriceRangeEur { get; init; }
    public List<string> TopAttractions { get; init; } = [];
    public List<string> KeyAmenities { get; init; } = [];
    /// <summary>Deep-dive endpoint on the turkeyai booking platform (if a matching resort ID is known).</summary>
    public string? BookingDeepDiveUrl { get; init; }
}

/// <summary>A single service recommendation (excursion, transfer, package, etc.).</summary>
public class ServiceRecommendation
{
    public required string Title { get; init; }
    public required string Description { get; init; }
    public string? PriceHint { get; init; }
    /// <summary>API endpoint on the turkeyai booking platform for this service type.</summary>
    public required string ServiceEndpoint { get; init; }
}

/// <summary>Response from <c>GET /api/booking/services</c> — the service-vertical catalogue.</summary>
public class TravelServiceCatalog
{
    public List<TravelServiceVertical> Verticals { get; init; } = [];
}

/// <summary>One travel service vertical available via the turkeyai booking platform.</summary>
public class TravelServiceVertical
{
    public required string Key { get; init; }
    public required string Title { get; init; }
    public required string Description { get; init; }
    public required string Icon { get; init; }
    public required string Endpoint { get; init; }
    public string? GdsSupplier { get; init; }
}
