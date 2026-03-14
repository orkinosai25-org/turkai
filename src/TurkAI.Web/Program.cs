using TurkAI.Web.Components;
using TurkAI.Web.Services;
using AspNet.Security.OAuth.LinkedIn;

var builder = WebApplication.CreateBuilder(args);

// ── Blazor Server ────────────────────────────────────────────────────────────
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// ── Razor Pages (required for OAuth challenge/callback endpoints) ─────────────
builder.Services.AddRazorPages();

// ── User service (in-memory store; swap for EF Core in production) ────────────
builder.Services.AddSingleton<IUserService, InMemoryUserService>();

// ── Advert service (in-memory store; swap for EF Core in production) ──────────
builder.Services.AddSingleton<IAdvertService, InMemoryAdvertService>();

// ── Authentication ────────────────────────────────────────────────────────────
var authBuilder = builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme = "TurkAI.Cookie";
        options.DefaultChallengeScheme = "TurkAI.Cookie";
    })
    .AddCookie("TurkAI.Cookie", options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/login";
        options.Cookie.Name = "TurkAI.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.ExpireTimeSpan = TimeSpan.FromDays(14);
        options.SlidingExpiration = true;
    })
    // External provider cookies (temporary, used during OAuth handshake)
    .AddCookie("ExternalCookie", options =>
    {
        options.Cookie.Name = "TurkAI.External";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(5);
    });

// Google OAuth (configure ClientId/ClientSecret in appsettings or Azure Key Vault)
var googleClientId = builder.Configuration["Auth:Google:ClientId"];
var googleClientSecret = builder.Configuration["Auth:Google:ClientSecret"];
if (!string.IsNullOrEmpty(googleClientId) && !string.IsNullOrEmpty(googleClientSecret))
{
    authBuilder.AddGoogle(options =>
    {
        options.SignInScheme = "ExternalCookie";
        options.ClientId = googleClientId;
        options.ClientSecret = googleClientSecret;
        options.CallbackPath = "/signin-google";
        options.SaveTokens = false;
    });
}

// Facebook OAuth
var fbAppId = builder.Configuration["Auth:Facebook:AppId"];
var fbAppSecret = builder.Configuration["Auth:Facebook:AppSecret"];
if (!string.IsNullOrEmpty(fbAppId) && !string.IsNullOrEmpty(fbAppSecret))
{
    authBuilder.AddFacebook(options =>
    {
        options.SignInScheme = "ExternalCookie";
        options.AppId = fbAppId;
        options.AppSecret = fbAppSecret;
        options.CallbackPath = "/signin-facebook";
        options.SaveTokens = false;
    });
}

// LinkedIn OAuth
var linkedInClientId = builder.Configuration["Auth:LinkedIn:ClientId"];
var linkedInClientSecret = builder.Configuration["Auth:LinkedIn:ClientSecret"];
if (!string.IsNullOrEmpty(linkedInClientId) && !string.IsNullOrEmpty(linkedInClientSecret))
{
    authBuilder.AddLinkedIn(options =>
    {
        options.SignInScheme = "ExternalCookie";
        options.ClientId = linkedInClientId;
        options.ClientSecret = linkedInClientSecret;
        options.CallbackPath = "/signin-linkedin";
        options.SaveTokens = false;
    });
}

builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

// ── HTTP client pointing to the TürkAI API ───────────────────────────────────
var apiBaseUrl = builder.Configuration["TurkAIApi:BaseUrl"]
    ?? "https://localhost:7200";

builder.Services.AddScoped(sp =>
    new HttpClient { BaseAddress = new Uri(apiBaseUrl) });

var app = builder.Build();

// ── HTTP pipeline ────────────────────────────────────────────────────────────
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorPages();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();


