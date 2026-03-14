using TurkAI.Web.Models;

namespace TurkAI.Web.Services;

/// <summary>
/// In-memory advertisement store for development / MVP.
/// Seeded with TürkAI own-brand promotions so ad slots are populated from day one.
/// Replace with a persistent store (e.g. EF Core + SQL) for production.
/// </summary>
public class InMemoryAdvertService : IAdvertService
{
    private readonly List<Advertisement> _ads;
    private readonly Lock _lock = new();

    public InMemoryAdvertService()
    {
        // Seed with TürkAI own-brand adverts
        _ads =
        [
            new Advertisement
            {
                Id = "seed-1",
                Title = "Discover Türkiye with AI",
                Description = "Plan your perfect Turkish holiday in seconds. Let our GPT-4o travel expert find hidden gems just for you.",
                TargetUrl = "/chat",
                AdvertiserName = "TürkiyeAI",
                Position = AdvertPosition.TopBanner,
                Type = AdvertType.Banner,
                IsActive = true,
                Priority = 10,
                CallToAction = "Start Chatting Free",
            },
            new Advertisement
            {
                Id = "seed-2",
                Title = "Unlock Video Insights",
                Description = "Drop in any travel video URL and get AI-powered destination summaries, keywords and transcripts.",
                TargetUrl = "/video",
                AdvertiserName = "TürkiyeAI",
                Position = AdvertPosition.ContentMiddle,
                Type = AdvertType.Card,
                IsActive = true,
                Priority = 5,
                CallToAction = "Try Video Insights",
            },
            new Advertisement
            {
                Id = "seed-3",
                Title = "Enterprise API — Embed TürkiyeAI on Your Website",
                Description = "Hotels, tour operators and travel agencies: integrate our Turkish travel AI directly into your platform.",
                TargetUrl = "/pricing",
                AdvertiserName = "TürkiyeAI",
                Position = AdvertPosition.FooterBanner,
                Type = AdvertType.Banner,
                IsActive = true,
                Priority = 5,
                CallToAction = "View Enterprise Plan",
            },
        ];
    }

    public Task<IReadOnlyList<Advertisement>> GetAllAsync()
    {
        lock (_lock)
        {
            return Task.FromResult<IReadOnlyList<Advertisement>>([.. _ads]);
        }
    }

    public Task<IReadOnlyList<Advertisement>> GetActiveByPositionAsync(AdvertPosition position)
    {
        var now = DateTime.UtcNow;
        lock (_lock)
        {
            var results = _ads
                .Where(a => a.IsActive
                    && a.Position == position
                    && (a.StartsAt == null || a.StartsAt <= now)
                    && (a.EndsAt == null || a.EndsAt >= now))
                .OrderByDescending(a => a.Priority)
                .ToList();
            return Task.FromResult<IReadOnlyList<Advertisement>>(results);
        }
    }

    public Task<Advertisement?> GetByIdAsync(string id)
    {
        lock (_lock)
        {
            return Task.FromResult(_ads.FirstOrDefault(a => a.Id == id));
        }
    }

    public Task<Advertisement> CreateAsync(Advertisement ad)
    {
        lock (_lock)
        {
            _ads.Add(ad);
        }
        return Task.FromResult(ad);
    }

    public Task<Advertisement> UpdateAsync(Advertisement ad)
    {
        lock (_lock)
        {
            var idx = _ads.FindIndex(a => a.Id == ad.Id);
            if (idx >= 0)
                _ads[idx] = ad;
        }
        return Task.FromResult(ad);
    }

    public Task DeleteAsync(string id)
    {
        lock (_lock)
        {
            _ads.RemoveAll(a => a.Id == id);
        }
        return Task.CompletedTask;
    }
}
