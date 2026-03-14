using System.ComponentModel.DataAnnotations;

namespace TurkAI.Web.Models;

/// <summary>Where an advertisement is displayed on the page.</summary>
public enum AdvertPosition
{
    /// <summary>Full-width banner at the top of the main content area.</summary>
    TopBanner,

    /// <summary>Card-style slot in the middle of a page's content body.</summary>
    ContentMiddle,

    /// <summary>Slim banner pinned to the page footer.</summary>
    FooterBanner,
}

/// <summary>Visual format of the advertisement.</summary>
public enum AdvertType
{
    /// <summary>Wide, image-led banner.</summary>
    Banner,

    /// <summary>Compact card with title, description and call-to-action.</summary>
    Card,
}

/// <summary>A single advertisement record.</summary>
public class Advertisement
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required(ErrorMessage = "Title is required.")]
    [MaxLength(120)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(300)]
    public string? Description { get; set; }

    [Url(ErrorMessage = "Image URL must be an absolute URL (e.g. https://…).")]
    public string? ImageUrl { get; set; }

    [Required(ErrorMessage = "Target URL is required.")]
    [MaxLength(500)]
    public string TargetUrl { get; set; } = "#";

    [MaxLength(120)]
    public string AdvertiserName { get; set; } = string.Empty;

    public AdvertPosition Position { get; set; } = AdvertPosition.TopBanner;
    public AdvertType Type { get; set; } = AdvertType.Banner;
    public bool IsActive { get; set; } = true;
    public DateTime? StartsAt { get; set; }
    public DateTime? EndsAt { get; set; }
    public int Priority { get; set; } = 0;

    [MaxLength(80)]
    public string CallToAction { get; set; } = "Learn More";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
