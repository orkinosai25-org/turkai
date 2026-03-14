namespace TurkAI.API.Middleware;

/// <summary>
/// Validates the <c>Authorization: Bearer tk_…</c> API key on inbound requests.
/// Requests to <c>/health</c> or <c>/openapi</c> are exempt.
/// When <c>ApiKeys:Required</c> is <c>false</c> (default in development), all requests pass through.
/// </summary>
public sealed class ApiKeyMiddleware
{
    private const string BearerPrefix = "Bearer ";
    private static readonly string[] ExemptPaths = ["/health", "/openapi"];

    private readonly RequestDelegate _next;
    private readonly ILogger<ApiKeyMiddleware> _logger;
    private readonly bool _required;
    private readonly HashSet<string> _validKeys;

    public ApiKeyMiddleware(RequestDelegate next, IConfiguration configuration, ILogger<ApiKeyMiddleware> logger)
    {
        _next = next;
        _logger = logger;
        _required = configuration.GetValue<bool>("ApiKeys:Required");
        var keys = configuration.GetSection("ApiKeys:Keys").Get<string[]>() ?? [];
        _validKeys = new HashSet<string>(keys, StringComparer.Ordinal);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Always allow exempt paths (health check, OpenAPI spec)
        if (!_required || ExemptPaths.Any(p => context.Request.Path.Equals(p, StringComparison.OrdinalIgnoreCase)
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
        if (authHeader is null || !authHeader.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("API request rejected — missing or malformed Authorization header from {IP}",
                context.Connection.RemoteIpAddress);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = "Missing or invalid API key." });
            return;
        }

        var key = authHeader[BearerPrefix.Length..].Trim();
        if (!_validKeys.Contains(key))
        {
            _logger.LogWarning("API request rejected — unknown API key from {IP}", context.Connection.RemoteIpAddress);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = "Invalid API key." });
            return;
        }

        await _next(context);
    }
}
