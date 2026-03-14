using Azure.AI.OpenAI;
using TurkAI.API.Middleware;
using TurkAI.API.Services;

var builder = WebApplication.CreateBuilder(args);

// ── Azure OpenAI (GPT-4o) ───────────────────────────────────────────────────
builder.Services.AddSingleton(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var endpoint = config["AzureOpenAI:Endpoint"]
        ?? throw new InvalidOperationException("AzureOpenAI:Endpoint is not configured.");
    var key = config["AzureOpenAI:Key"]
        ?? throw new InvalidOperationException("AzureOpenAI:Key is not configured.");
    return new AzureOpenAIClient(new Uri(endpoint), new Azure.AzureKeyCredential(key));
});
builder.Services.AddScoped<IAzureOpenAIService, AzureOpenAIService>();

// ── Azure AI Service integrations ───────────────────────────────────────────
builder.Services.AddScoped<ITranslatorService, TranslatorService>();       // Azure Translator
builder.Services.AddScoped<ISpeechService, SpeechService>();               // Azure Speech
builder.Services.AddScoped<ILanguageService, LanguageService>();           // Azure Language / NLP
builder.Services.AddScoped<IComputerVisionService, ComputerVisionService>(); // Azure Computer Vision
builder.Services.AddScoped<IVideoIndexerService, VideoIndexerService>();   // Azure Video Indexer
builder.Services.AddScoped<IPersonalisationService, PersonalisationService>(); // Azure Personalizer

// ── HTTP clients ────────────────────────────────────────────────────────────
builder.Services.AddHttpClient("VideoIndexer");

// ── MVC / OpenAPI ───────────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// ── CORS (allow Blazor frontend and MERN stack origin in dev) ───────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("TurkAICors", policy =>
    {
        policy.WithOrigins(
                builder.Configuration["AllowedOrigins"] ?? "https://localhost:7001")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// ── Middleware pipeline ──────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors("TurkAICors");
app.UseMiddleware<ApiKeyMiddleware>();
app.UseAuthorization();
app.MapControllers();

app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "TürkAI API" }))
   .WithName("HealthCheck");

app.Run();
