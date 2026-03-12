# 🇹🇷 TürkAI — AI-Powered Turkey Travel Platform

TürkAI is a **SaaS AI platform** that makes Türkiye's travel industry smarter. It provides a GPT-4o–powered travel assistant with deep Turkish expertise, integrated with six Azure AI services and deployed on Azure Web Apps + Azure Functions.

---

## Architecture

```
┌──────────────────────┐        ┌──────────────────────────────────────────────┐
│  TurkAI.Web          │        │  TurkAI.API  (ASP.NET Core Web API)          │
│  Blazor Server App   │──────▶ │  GPT-4o • 4 AI Agent Tools • 6 AI Services  │
│  (Azure Web App)     │        │  (Azure Web App)                             │
└──────────────────────┘        └────────────┬─────────────────────────────────┘
                                             │
                     ┌───────────────────────┼───────────────────────┐
                     ▼                       ▼                       ▼
          TurkAI.Functions         Azure AI Services          Azure Infra
          (Azure Functions v4)     ─ Azure OpenAI GPT-4o     ─ Key Vault
          ─ ChatOrchestrator       ─ Azure Translator        ─ App Insights
          ─ VideoProcessor         ─ Azure Speech            ─ Storage Account
                                   ─ Azure Language (NLP)
                                   ─ Azure Computer Vision
                                   ─ Azure Video Indexer
                                   ─ Azure Personalizer
```

---

## Projects

| Project | Type | Description |
|---|---|---|
| `TurkAI.Web` | Blazor Server | User-facing SaaS frontend |
| `TurkAI.API` | ASP.NET Core Web API | AI orchestration backend |
| `TurkAI.Functions` | Azure Functions v4 | Serverless chat + video processing |
| `TurkAI.Shared` | Class Library | Shared models, AI tool definitions |

---

## AI Features

### GPT-4o Function-Calling Pipeline — 4 Agent Tools

| Tool | Description |
|---|---|
| `get_travel_info` | Structured destination data: highlights, tips, best season, currency |
| `translate_content` | Real-time Turkish ↔ English translation |
| `analyse_image` | Landmark & caption extraction from travel image URLs |
| `get_video_insights` | Video URL ingestion → scenes, keywords, destinations, transcript |

### 6 Azure AI Service Integrations

| Service | Azure Resource | Use Case |
|---|---|---|
| **Azure OpenAI (GPT-4o)** | `Azure AI Foundry` | Chat, content generation, tool dispatch |
| **Azure Translator** | `Microsoft.CognitiveServices/TextTranslation` | Turkish ↔ English translation |
| **Azure Speech** | `Microsoft.CognitiveServices/SpeechServices` | Text-to-speech (Turkish voices), transcription |
| **Azure Language (NLP)** | `Microsoft.CognitiveServices/TextAnalytics` | Key phrases, sentiment, NER |
| **Azure Computer Vision** | `Microsoft.CognitiveServices/ComputerVision` | Image captions, tags, landmark detection |
| **Azure Video Indexer** | Azure Video Indexer REST API | Video URL ingestion, transcript, keywords |
| **Azure Personalizer** | `Microsoft.CognitiveServices/Personalizer` | ML-based destination recommendations |

---

## API Endpoints

| Endpoint | Method | Description |
|---|---|---|
| `POST /api/chat` | Chat | GPT-4o chat with function-calling |
| `POST /api/translation/translate` | Translate | Turkish ↔ English |
| `POST /api/translation/detect` | Language detection | Auto-detect language |
| `POST /api/translation/keyphrases` | NLP | Key phrase extraction |
| `POST /api/translation/sentiment` | NLP | Sentiment analysis |
| `POST /api/image/analyse` | Vision | Image analysis |
| `POST /api/video/ingest` | Video | Ingest video URL |
| `GET /api/video/{id}/insights` | Video | Retrieve video insights |
| `POST /api/speech/synthesise` | Speech | Text-to-speech (WAV) |
| `POST /api/speech/transcribe` | Speech | Speech-to-text |
| `POST /api/personalisation/recommendations` | ML | Personalised destinations |
| `POST /api/personalisation/feedback` | ML | Record recommendation feedback |
| `GET /health` | Health | Health check |

---

## Configuration

All secrets use `appsettings.json` placeholders and must be set via **Azure Key Vault** or environment variables in production. Never commit real keys.

### appsettings.json placeholders

```json
{
  "AzureOpenAI":      { "Endpoint": "__AZURE_OPENAI_ENDPOINT__",    "Key": "__AZURE_OPENAI_KEY__",    "DeploymentName": "gpt-4o" },
  "AzureTranslator":  { "Key": "__AZURE_TRANSLATOR_KEY__",          "Region": "__AZURE_TRANSLATOR_REGION__" },
  "AzureSpeech":      { "Key": "__AZURE_SPEECH_KEY__",              "Region": "__AZURE_SPEECH_REGION__" },
  "AzureLanguage":    { "Endpoint": "__AZURE_LANGUAGE_ENDPOINT__",  "Key": "__AZURE_LANGUAGE_KEY__" },
  "AzureVision":      { "Endpoint": "__AZURE_VISION_ENDPOINT__",    "Key": "__AZURE_VISION_KEY__" },
  "AzureVideoIndexer":{ "AccountId": "__...__", "Location": "__...__", "SubscriptionKey": "__...__" },
  "AzurePersonalizer":{ "Endpoint": "__AZURE_PERSONALIZER_ENDPOINT__", "Key": "__AZURE_PERSONALIZER_KEY__" }
}
```

### Azure Functions `local.settings.json` (process.env mappings)

```json
{
  "Values": {
    "AzureOpenAI__Endpoint": "",
    "AzureOpenAI__Key": "",
    "AzureTranslator__Key": "",
    "AzureVision__Endpoint": "",
    "AzureVideoIndexer__AccountId": ""
  }
}
```

---

## Quick Start (Development)

### Prerequisites
- .NET 10 SDK
- Azure subscription + resources provisioned via Bicep
- (Optional) Azure Functions Core Tools v4

### 1. Provision Azure Infrastructure

```bash
az group create -n turkai-rg -l westeurope
az deployment group create \
  -g turkai-rg \
  -f infra/main.bicep \
  -p projectName=turkai
```

### 2. Configure secrets (local development)

Populate the empty strings in `src/TurkAI.API/appsettings.Development.json` and `src/TurkAI.Functions/local.settings.json` with your Azure resource keys.

### 3. Run the API

```bash
cd src/TurkAI.API
dotnet run
# API available at https://localhost:7200
```

### 4. Run the Blazor frontend

```bash
cd src/TurkAI.Web
dotnet run
# Web app available at https://localhost:7001
```

### 5. Run Azure Functions locally

```bash
cd src/TurkAI.Functions
func start
# Functions available at http://localhost:7071
```

---

## CI/CD

The `.github/workflows/azure-deploy.yml` pipeline:

1. **Builds and tests** the solution on every push/PR
2. **Deploys Bicep infrastructure** on merge to `main`
3. **Deploys API, Web, and Functions** to Azure Web Apps

### Required GitHub Secrets

| Secret | Description |
|---|---|
| `AZURE_CREDENTIALS` | Azure service principal JSON |
| `AZURE_API_APP_NAME` | API Web App name |
| `AZURE_WEB_APP_NAME` | Blazor Web App name |
| `AZURE_FUNCTIONS_APP_NAME` | Functions App name |

---

## MERN Stack Integration

The TürkAI API is a standard REST+JSON API — any frontend (React/Next.js MERN stack, mobile app, or third-party travel site) can call it directly. Use the `process.env` mappings in `local.settings.json` to configure the Functions endpoint for Node.js consumers.

---

## Licence

MIT
