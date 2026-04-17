using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using PmTool.Api.Models;

namespace PmTool.Api.Services;

/// <summary>
/// Primary LLM integration — calls Anthropic Claude claude-sonnet-4-6 via REST API.
/// Falls back gracefully when ApiKey is not configured (dev / demo mode).
/// </summary>
public class AnthropicService
{
    private readonly IHttpClientFactory _http;
    private readonly IConfiguration _config;
    private readonly ILogger<AnthropicService> _log;

    private const string BaseUrl = "https://api.anthropic.com/v1/messages";
    private const string AnthropicVersion = "2023-06-01";

    public AnthropicService(IHttpClientFactory http, IConfiguration config, ILogger<AnthropicService> log)
    {
        _http = http;
        _config = config;
        _log = log;
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_config["Anthropic:ApiKey"]);

    /// <summary>
    /// Send a single prompt and get a text response.
    /// </summary>
    public async Task<string> CompleteAsync(string systemPrompt, string userMessage, int maxTokens = 2048)
    {
        if (!IsConfigured)
            return "[AI not configured — set Anthropic:ApiKey in appsettings]";

        var messages = new[] { new { role = "user", content = userMessage } };
        return await CallAsync(systemPrompt, messages, maxTokens);
    }

    /// <summary>
    /// Send a multi-turn conversation and get a text response.
    /// </summary>
    public async Task<string> ChatAsync(string systemPrompt, List<(string Role, string Content)> history, int maxTokens = 2048)
    {
        if (!IsConfigured)
            return "[AI not configured — set Anthropic:ApiKey in appsettings]";

        var messages = history.Select(h => new { role = h.Role, content = h.Content }).ToArray();
        return await CallAsync(systemPrompt, messages, maxTokens);
    }

    /// <summary>
    /// Extract structured JSON from text using Claude.
    /// Returns null on failure.
    /// </summary>
    public async Task<T?> ExtractStructuredAsync<T>(string systemPrompt, string userMessage, int maxTokens = 2048)
    {
        var raw = await CompleteAsync(systemPrompt, userMessage, maxTokens);
        if (raw.StartsWith("[AI not configured")) return default;

        try
        {
            // Strip markdown code fences if present
            var json = raw.Trim();
            if (json.StartsWith("```json")) json = json[7..];
            if (json.StartsWith("```")) json = json[3..];
            if (json.EndsWith("```")) json = json[..^3];
            json = json.Trim();

            return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                AllowTrailingCommas = true
            });
        }
        catch (Exception ex)
        {
            _log.LogWarning("Failed to deserialize Claude response: {Error}\nRaw: {Raw}", ex.Message, raw);
            return default;
        }
    }

    private async Task<string> CallAsync(string systemPrompt, object messages, int maxTokens)
    {
        var apiKey = _config["Anthropic:ApiKey"]!;
        var model = _config["Anthropic:Model"] ?? "claude-sonnet-4-6";

        var payload = new
        {
            model,
            max_tokens = maxTokens,
            system = systemPrompt,
            messages
        };

        var json = JsonSerializer.Serialize(payload);
        var client = _http.CreateClient("anthropic");

        using var request = new HttpRequestMessage(HttpMethod.Post, BaseUrl);
        request.Headers.Add("x-api-key", apiKey);
        request.Headers.Add("anthropic-version", AnthropicVersion);
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            using var response = await client.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _log.LogError("Anthropic API error {Status}: {Body}", response.StatusCode, body);
                return $"[AI error: {response.StatusCode}]";
            }

            var doc = JsonDocument.Parse(body);
            return doc.RootElement
                .GetProperty("content")[0]
                .GetProperty("text")
                .GetString() ?? "";
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Anthropic API call failed");
            return "[AI service temporarily unavailable]";
        }
    }
}

// ─── Extraction result types ─────────────────────────────────────────────────

public class MeetingExtractionResult
{
    public List<ExtractedTask> Tasks { get; set; } = [];
    public List<ExtractedDecision> Decisions { get; set; } = [];
    public List<ExtractedRisk> Risks { get; set; } = [];
    public List<ExtractedKnowledge> Knowledge { get; set; } = [];
    public string Summary { get; set; } = "";
    public string Sentiment { get; set; } = "neutral"; // positive | neutral | concerning
    public float Confidence { get; set; } = 0.8f;
}

public class ExtractedTask
{
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string AssigneeName { get; set; } = "";
    public string Priority { get; set; } = "medium"; // high | medium | low
    public int DueDaysFromNow { get; set; } = 7;
}

public class ExtractedDecision
{
    public string Title { get; set; } = "";
    public string Context { get; set; } = "";
    public string OwnerName { get; set; } = "";
    public int DueDaysFromNow { get; set; } = 5;
}

public class ExtractedRisk
{
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string OwnerName { get; set; } = "";
    public int Impact { get; set; } = 3;
    public int Probability { get; set; } = 3;
}

public class ExtractedKnowledge
{
    public string Title { get; set; } = "";
    public string Content { get; set; } = "";
    public string Category { get; set; } = "meeting";
    public int Importance { get; set; } = 4;
}

public class NlTaskResult
{
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string AssigneeName { get; set; } = "";
    public string Priority { get; set; } = "medium";
    public int DueDaysFromNow { get; set; } = 7;
    public float Confidence { get; set; } = 0.9f;
}

public class DeadlinePrediction
{
    public DateOnly PredictedDeliveryDate { get; set; }
    public int ConfidencePercent { get; set; }
    public string Reasoning { get; set; } = "";
    public string Trend { get; set; } = "on_track"; // on_track | at_risk | delayed
    public int DelayDays { get; set; }
}

public class SentimentAnalysis
{
    public string Overall { get; set; } = "neutral";
    public List<ParticipantSentiment> Participants { get; set; } = [];
    public List<string> ConcernTopics { get; set; } = [];
    public int ConcernCount { get; set; }
}

public class ParticipantSentiment
{
    public string Name { get; set; } = "";
    public string Sentiment { get; set; } = "neutral";
    public int ConcernCount { get; set; }
}
