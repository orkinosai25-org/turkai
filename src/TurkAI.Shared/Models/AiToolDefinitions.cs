using OpenAI.Chat;

namespace TurkAI.Shared.Models;

/// <summary>Defines the four AI agent tools exposed to the GPT-4o function-calling pipeline.</summary>
public static class AiToolDefinitions
{
    /// <summary>Tool: retrieve structured Turkish travel information for a destination.</summary>
    public static ChatTool GetTravelInfo { get; } = ChatTool.CreateFunctionTool(
        functionName: "get_travel_info",
        functionDescription: "Retrieve structured travel information about a Turkish destination, including highlights, practical tips, best season, currency, and local language notes.",
        functionParameters: BinaryData.FromString("""
        {
          "type": "object",
          "properties": {
            "destination": {
              "type": "string",
              "description": "The Turkish city or region to look up (e.g. 'Istanbul', 'Cappadocia', 'Antalya')."
            },
            "language": {
              "type": "string",
              "enum": ["en", "tr"],
              "description": "Language for the returned information.",
              "default": "en"
            },
            "interests": {
              "type": "array",
              "items": { "type": "string" },
              "description": "Optional list of traveller interests (e.g. ['history','food','beaches'])."
            }
          },
          "required": ["destination"]
        }
        """)
    );

    /// <summary>Tool: translate text between Turkish and English.</summary>
    public static ChatTool TranslateContent { get; } = ChatTool.CreateFunctionTool(
        functionName: "translate_content",
        functionDescription: "Translate text between Turkish (tr) and English (en), or detect the source language automatically.",
        functionParameters: BinaryData.FromString("""
        {
          "type": "object",
          "properties": {
            "text": {
              "type": "string",
              "description": "The text to translate."
            },
            "target_language": {
              "type": "string",
              "enum": ["en", "tr"],
              "description": "Target language code."
            },
            "source_language": {
              "type": "string",
              "enum": ["en", "tr"],
              "description": "Source language code. Omit to auto-detect."
            }
          },
          "required": ["text", "target_language"]
        }
        """)
    );

    /// <summary>Tool: analyse a travel-related image and identify landmarks or destinations.</summary>
    public static ChatTool AnalyseImage { get; } = ChatTool.CreateFunctionTool(
        functionName: "analyse_image",
        functionDescription: "Analyse a travel image URL to identify landmarks, locations, tags, and a descriptive caption using Azure Computer Vision.",
        functionParameters: BinaryData.FromString("""
        {
          "type": "object",
          "properties": {
            "image_url": {
              "type": "string",
              "format": "uri",
              "description": "Publicly accessible URL of the image to analyse."
            },
            "language": {
              "type": "string",
              "enum": ["en", "tr"],
              "description": "Language for returned captions and tags.",
              "default": "en"
            }
          },
          "required": ["image_url"]
        }
        """)
    );

    /// <summary>Tool: ingest a travel video URL and extract insights using Azure Video Indexer.</summary>
    public static ChatTool GetVideoInsights { get; } = ChatTool.CreateFunctionTool(
        functionName: "get_video_insights",
        functionDescription: "Submit a travel video URL for indexing and return AI-extracted insights: scenes, keywords, destinations mentioned, and a summary. Supports both Turkish and English content.",
        functionParameters: BinaryData.FromString("""
        {
          "type": "object",
          "properties": {
            "video_url": {
              "type": "string",
              "format": "uri",
              "description": "Publicly accessible URL of the video to index (MP4, YouTube, or Azure Blob)."
            },
            "language": {
              "type": "string",
              "enum": ["en", "tr"],
              "description": "Primary spoken language in the video.",
              "default": "en"
            },
            "name": {
              "type": "string",
              "description": "Optional human-readable name for the video."
            }
          },
          "required": ["video_url"]
        }
        """)
    );

    /// <summary>Tool: search hotels and resorts near a Turkish destination with personalised recommendations.</summary>
    public static ChatTool GetHotelRecommendations { get; } = ChatTool.CreateFunctionTool(
        functionName: "get_hotel_recommendations",
        functionDescription: "Find recommended hotels, resorts, and accommodation options near a Turkish destination. Returns options ranked by category (luxury, boutique, budget) with details on vicinity, amenities, and nearby attractions.",
        functionParameters: BinaryData.FromString("""
        {
          "type": "object",
          "properties": {
            "destination": {
              "type": "string",
              "description": "The Turkish city or region to find hotels near (e.g. 'Antalya', 'Bodrum', 'Cappadocia')."
            },
            "category": {
              "type": "string",
              "enum": ["luxury", "boutique", "mid-range", "budget", "all"],
              "description": "Accommodation category filter.",
              "default": "all"
            },
            "interests": {
              "type": "array",
              "items": { "type": "string" },
              "description": "Traveller interests to tailor recommendations (e.g. ['beach','history','spa','golf'])."
            },
            "language": {
              "type": "string",
              "enum": ["en", "tr"],
              "description": "Language for the returned information.",
              "default": "en"
            }
          },
          "required": ["destination"]
        }
        """)
    );

    /// <summary>All tools as a list, ready to pass to the OpenAI chat client.</summary>
    public static IReadOnlyList<ChatTool> All { get; } =
        [GetTravelInfo, GetHotelRecommendations, TranslateContent, AnalyseImage, GetVideoInsights];
}
