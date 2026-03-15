namespace TurkAI.API.Middleware;

/// <summary>
/// Validates the <c>Authorization: Bearer …</c> API key on inbound requests and sets the API
/// key tier on <see cref="HttpContext.Items"/> so controllers can enforce tier-based access.
///
/// Two key tiers are supported:
/// <list type="bullet">
///   <item><term>partner</term><description>Private keys for the owner's own TürkiyeAI booking SaaS.
///   Grants access to partner-only endpoints such as <c>/api/booking/*</c>.</description></item>
///   <item><term>enterprise</term><description>Keys issued to external Enterprise subscribers for
///   public API and widget integration.</description></item>
/// </list>
///
/// Requests to <c>/health</c> or <c>/openapi</c> are always exempt.
/// When <c>ApiKeys:Required</c> is <c>false</c> (default in development) all requests pass through,
/// but the tier is still resolved when a valid key is present.
/// </summary>
public sealed class ApiKeyMiddleware
{
    private const string BearerPrefix = "Bearer ";

    /// <summary>HttpContext.Items key that carries the resolved tier string.</summary>
    public const string TierItemKey = "ApiKeyTier";

    /// <summary>Tier values written to <see cref="HttpContext.Items"/>.</summary>
    public const string TierPartner = "partner";
    public const string TierEnterprise = "enterprise";

    private static readonly string[] ExemptPaths = ["/health", "/openapi"];

    private readonly RequestDelegate _next;
    private readonly ILogger<ApiKeyMiddleware> _logger;
    private readonly bool _required;
    private readonly HashSet<string> _enterpriseKeys;
    private readonly HashSet<string> _partnerKeys;

    public ApiKeyMiddleware(RequestDelegate next, IConfiguration configuration, ILogger<ApiKeyMiddleware> logger)
    {
        _next = next;
        _logger = logger;
        _required = configuration.GetValue<bool>("ApiKeys:Required");

        var enterprise = configuration.GetSection("ApiKeys:Keys").Get<string[]>() ?? [];
        _enterpriseKeys = new HashSet<string>(enterprise, StringComparer.Ordinal);

        var partner = configuration.GetSection("ApiKeys:PartnerKeys").Get<string[]>() ?? [];
        _partnerKeys = new HashSet<string>(partner, StringComparer.Ordinal);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Always allow exempt paths (health check, OpenAPI spec)
        if (ExemptPaths.Any(p => context.Request.Path.Equals(p, StringComparison.OrdinalIgnoreCase)
                                 || context.Request.Path.StartsWithSegments(p + "/", StringComparison.OrdinalIgnoreCase)))
        {
            await _next(context);
            return;
        }

        // CORS pre-flight — must not be blocked
        if (HttpMethods.IsOptions(context.Request.Method))
        {
            await _next(context);
            return;
        }

        var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
        if (authHeader is not null && authHeader.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var key = authHeader[BearerPrefix.Length..].Trim();

            if (_partnerKeys.Contains(key))
            {
                context.Items[TierItemKey] = TierPartner;
            }
            else if (_enterpriseKeys.Contains(key))
            {
                context.Items[TierItemKey] = TierEnterprise;
            }
        }

        // If auth is required and no tier was resolved, reject the request
        if (_required && !context.Items.ContainsKey(TierItemKey))
        {
            var hasHeader = authHeader is not null && authHeader.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase);
            if (hasHeader)
            {
                _logger.LogWarning("API request rejected — invalid API key from {IP}", context.Connection.RemoteIpAddress);
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new { error = "Invalid API key." });
            }
            else
            {
                _logger.LogWarning("API request rejected — missing Authorization header from {IP}", context.Connection.RemoteIpAddress);
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new { error = "Missing or invalid API key." });
            }
            return;
        }

        await _next(context);
    }
}
