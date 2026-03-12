using Azure.AI.OpenAI;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

// ── Azure OpenAI client (shared across functions) ────────────────────────────
builder.Services.AddSingleton(_ =>
{
    var endpoint = Environment.GetEnvironmentVariable("AzureOpenAI__Endpoint")
        ?? throw new InvalidOperationException("AzureOpenAI__Endpoint environment variable is not set.");
    var key = Environment.GetEnvironmentVariable("AzureOpenAI__Key")
        ?? throw new InvalidOperationException("AzureOpenAI__Key environment variable is not set.");
    return new AzureOpenAIClient(new Uri(endpoint), new Azure.AzureKeyCredential(key));
});

// ── HTTP client for Video Indexer ────────────────────────────────────────────
builder.Services.AddHttpClient("VideoIndexer");

builder.Build().Run();
