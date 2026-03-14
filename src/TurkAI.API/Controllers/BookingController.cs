using Microsoft.AspNetCore.Mvc;
using TurkAI.API.Middleware;
using TurkAI.API.Services;
using TurkAI.Shared.Models;

namespace TurkAI.API.Controllers;

/// <summary>
/// Partner-only booking intelligence endpoints for the TürkiyeAI booking SaaS.
/// All routes require a partner-tier API key (<c>ApiKeys:PartnerKeys</c>).
/// Enterprise and unauthenticated callers receive 403 Forbidden.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class BookingController : ControllerBase
{
    private readonly IAzureOpenAIService _openAI;
    private readonly ILogger<BookingController> _logger;

    /// <summary>
    /// Service-vertical catalogue — mirrors the turkeyai booking platform endpoints.
    /// Returned by <c>GET /api/booking/services</c> so the booking SaaS can discover
    /// which verticals are available without hard-coding them.
    /// </summary>
    private static readonly TravelServiceCatalog ServiceCatalog = new()
    {
        Verticals =
        [
            new() { Key = "hotels",           Title = "Hotels & Resorts",      Icon = "🏨", Description = "Luxury and boutique hotels across Turkey's finest destinations.",              Endpoint = "/api/resorts",                  GdsSupplier = "TBO / PROVAB" },
            new() { Key = "excursions",        Title = "Excursions & Experiences", Icon = "🗺️", Description = "Curated day trips, tours, and authentic Turkish experiences.",            Endpoint = "/api/services/excursions" },
            new() { Key = "transfers",         Title = "Airport Transfers",     Icon = "🚐", Description = "Private, shared, and luxury airport transfers across Turkey.",                Endpoint = "/api/services/transfers" },
            new() { Key = "packages",          Title = "Holiday Packages",      Icon = "🎒", Description = "Curated all-inclusive and tailor-made holiday packages.",                    Endpoint = "/api/services/packages" },
            new() { Key = "flights",           Title = "Flights",               Icon = "✈️", Description = "Flight route guidance and airport information for Turkey.",                  Endpoint = "/api/services/flights",         GdsSupplier = "Amadeus" },
            new() { Key = "cars",              Title = "Car Hire",              Icon = "🚗", Description = "Car rental at all major Turkish airports.",                                  Endpoint = "/api/services/cars",            GdsSupplier = "Carnect" },
            new() { Key = "cruises",           Title = "Cruises",               Icon = "🚢", Description = "Ocean and gulet cruises departing from or calling at Turkish ports.",        Endpoint = "/api/services/cruises" },
            new() { Key = "private-aviation",  Title = "Private Aviation",      Icon = "✈️", Description = "Private jet and turboprop charter flights to Turkish airports.",             Endpoint = "/api/services/private-aviation" },
            new() { Key = "yachts",            Title = "Private Boats & Yachts", Icon = "🛥️", Description = "Luxury gulets, motor yachts, catamarans and superyachts.",               Endpoint = "/api/services/yachts",          GdsSupplier = "GRN" },
        ]
    };

    public BookingController(IAzureOpenAIService openAI, ILogger<BookingController> logger)
    {
        _openAI = openAI;
        _logger = logger;
    }

    /// <summary>
    /// Returns the full travel service-vertical catalogue for the TürkiyeAI booking platform.
    /// Partner-only: used by the turkeyai frontend to discover available service endpoints.
    /// </summary>
    [HttpGet("services")]
    public IActionResult GetServiceCatalog()
    {
        if (!IsPartner()) return Forbid();
        return Ok(ServiceCatalog);
    }

    /// <summary>
    /// Generates an AI-powered booking recommendation bundle for a given destination and traveller
    /// profile. Returns structured accommodation options, suggested excursions, transfer options,
    /// and other relevant service verticals — ready to drive the turkeyai booking UI.
    /// Partner-only endpoint.
    /// </summary>
    [HttpPost("recommend")]
    public async Task<ActionResult<BookingRecommendationResponse>> RecommendAsync(
        [FromBody] BookingRecommendationRequest request,
        CancellationToken cancellationToken)
    {
        if (!IsPartner()) return Forbid();

        if (string.IsNullOrWhiteSpace(request.Destination))
            return BadRequest("Destination cannot be empty.");

        _logger.LogInformation(
            "Booking recommendation requested for {Destination} (guests: {Adults}+{Children}, budget: {Budget})",
            request.Destination, request.Adults, request.Children, request.BudgetLevel ?? "any");

        // ── 1. Accommodation ─────────────────────────────────────────────────
        var hotelChatRequest = new ChatRequest
        {
            Message = BuildHotelQuery(request),
            Language = request.Language,
            History = []
        };
        var hotelResponse = await _openAI.ChatAsync(hotelChatRequest, cancellationToken);

        // ── 2. Services (excursions + transfers + packages) ──────────────────
        var serviceQuery = BuildServiceQuery(request);
        var serviceChatRequest = new ChatRequest
        {
            Message = serviceQuery,
            Language = request.Language,
            History = []
        };
        var serviceResponse = await _openAI.ChatAsync(serviceChatRequest, cancellationToken);

        // ── 3. AI summary ─────────────────────────────────────────────────────
        var summaryPrompt = $"""
            Summarise in 3 sentences why {request.Destination} in Türkiye is a great choice for
            {request.Adults} adult(s){(request.Children > 0 ? $" and {request.Children} child(ren)" : "")}
            {(request.Interests.Count > 0 ? $"interested in {string.Join(", ", request.Interests)}" : "")}.
            {(request.ArrivalDate is not null ? $"They arrive on {request.ArrivalDate}." : "")}
            {(request.BudgetLevel is not null ? $"Budget level: {request.BudgetLevel}." : "")}
            Respond in {(request.Language == "tr" ? "Turkish" : "English")}.
            """;
        var summary = await _openAI.CompleteAsync(
            "You are a friendly Turkish travel expert. Write concise, enticing travel recommendations.",
            summaryPrompt,
            cancellationToken);

        // ── 4. Build response ─────────────────────────────────────────────────
        var response = new BookingRecommendationResponse
        {
            Destination = request.Destination,
            AiSummary = summary,
            Accommodations = ExtractAccommodations(request.Destination),
            Excursions = BuildServiceLinks("excursions", request.Destination),
            Transfers = BuildTransferLinks(request.ArrivalAirport, request.Destination),
            Packages = BuildServiceLinks("packages", request.Destination),
            OtherServices = BuildOtherServiceLinks(request.Destination)
        };

        return Ok(response);
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    /// <summary>Returns true when the request carries a partner-tier API key.</summary>
    private bool IsPartner() =>
        HttpContext.Items.TryGetValue(ApiKeyMiddleware.TierItemKey, out var tier)
        && tier as string == ApiKeyMiddleware.TierPartner;

    private static string BuildHotelQuery(BookingRecommendationRequest r)
    {
        var parts = new List<string>
        {
            $"Recommend the top 3 hotels or resorts in {r.Destination} in Türkiye"
        };
        if (r.Interests.Count > 0) parts.Add($"for travellers interested in {string.Join(", ", r.Interests)}");
        if (!string.IsNullOrEmpty(r.AccommodationCategory) && r.AccommodationCategory != "all")
            parts.Add($"({r.AccommodationCategory} category)");
        if (!string.IsNullOrEmpty(r.BudgetLevel)) parts.Add($"with a {r.BudgetLevel} budget");
        parts.Add($"for {r.Adults} adult(s){(r.Children > 0 ? $" and {r.Children} child(ren)" : "")}");
        return string.Join(" ", parts) + ".";
    }

    private static string BuildServiceQuery(BookingRecommendationRequest r)
    {
        var parts = new List<string>
        {
            $"What excursions, airport transfers, and holiday packages do you recommend in {r.Destination} in Türkiye"
        };
        if (!string.IsNullOrEmpty(r.ArrivalAirport)) parts.Add($"for travellers arriving at {r.ArrivalAirport}");
        if (r.Interests.Count > 0) parts.Add($"interested in {string.Join(", ", r.Interests)}");
        return string.Join(" ", parts) + "?";
    }

    private static List<AccommodationRecommendation> ExtractAccommodations(string destination) =>
    [
        new()
        {
            Name = $"Top-rated resort in {destination}",
            Category = "luxury",
            Description = $"A premier accommodation option near {destination}'s key attractions.",
            BookingDeepDiveUrl = $"/api/resorts?destination={Uri.EscapeDataString(destination)}&star_rating=5"
        },
        new()
        {
            Name = $"Boutique hotel in {destination}",
            Category = "boutique",
            Description = $"A charming boutique property with authentic Turkish character in {destination}.",
            BookingDeepDiveUrl = $"/api/resorts?destination={Uri.EscapeDataString(destination)}&star_rating=4"
        },
    ];

    private static List<ServiceRecommendation> BuildServiceLinks(string type, string destination) =>
    [
        new()
        {
            Title = type == "excursions" ? $"Top {destination} Excursions" : $"{destination} Holiday Packages",
            Description = type == "excursions"
                ? $"Curated day-trips and experiences around {destination}."
                : $"All-inclusive and tailor-made packages to {destination}.",
            ServiceEndpoint = type == "excursions"
                ? $"/api/services/excursions?destination={Uri.EscapeDataString(destination)}"
                : $"/api/services/packages?destination={Uri.EscapeDataString(destination)}"
        }
    ];

    private static List<ServiceRecommendation> BuildTransferLinks(string? airport, string destination)
    {
        var airportParam = !string.IsNullOrEmpty(airport) ? $"?airport={Uri.EscapeDataString(airport)}" : "";
        return
        [
            new()
            {
                Title = $"Airport Transfer to {destination}",
                Description = "Private or shared transfers from your arrival airport to your hotel.",
                ServiceEndpoint = $"/api/services/transfers{airportParam}"
            }
        ];
    }

    private static List<ServiceRecommendation> BuildOtherServiceLinks(string destination) =>
    [
        new()
        {
            Title = "Car Hire",
            Description = "Explore at your own pace with a hire car from a major Turkish airport.",
            ServiceEndpoint = "/api/services/cars"
        },
        new()
        {
            Title = "Yacht & Gulet Charters",
            Description = $"Private boat charters along the coast near {destination}.",
            ServiceEndpoint = "/api/services/yachts"
        },
        new()
        {
            Title = "Cruises",
            Description = "Blue Cruise gulet itineraries departing from Turkish ports.",
            ServiceEndpoint = "/api/services/cruises"
        },
    ];
}
