using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PmTool.Api.Models;

namespace PmTool.Api.Services;

/// <summary>
/// Generates and searches vector embeddings using OpenAI text-embedding-3-small.
/// Embeddings stored as JSON in EmbeddingVector table (DB-agnostic).
/// Cosine similarity computed in-memory — suitable for <50K vectors per tenant.
/// Migration path: move EmbeddingJson to pgvector VECTOR(1536) column for scale.
/// </summary>
public class EmbeddingService
{
    private readonly IHttpClientFactory _http;
    private readonly IConfiguration _config;
    private readonly AppDbContext _db;
    private readonly ILogger<EmbeddingService> _log;

    private const string OpenAiEmbeddingUrl = "https://api.openai.com/v1/embeddings";

    public EmbeddingService(IHttpClientFactory http, IConfiguration config, AppDbContext db, ILogger<EmbeddingService> log)
    {
        _http = http;
        _config = config;
        _db = db;
        _log = log;
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_config["OpenAI:ApiKey"]);

    // ─── Generate embedding ───────────────────────────────────────────────────

    public async Task<float[]?> GenerateEmbeddingAsync(string text)
    {
        if (!IsConfigured || string.IsNullOrWhiteSpace(text)) return null;

        var apiKey = _config["OpenAI:ApiKey"]!;
        var model = _config["OpenAI:EmbeddingModel"] ?? "text-embedding-3-small";

        // Truncate to ~8000 chars to stay within token limits
        var truncated = text.Length > 8000 ? text[..8000] : text;

        var payload = new { model, input = truncated };
        var json = JsonSerializer.Serialize(payload);

        var client = _http.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        try
        {
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await client.PostAsync(OpenAiEmbeddingUrl, content);
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _log.LogError("OpenAI embedding error {Status}: {Body}", response.StatusCode, body);
                return null;
            }

            var doc = JsonDocument.Parse(body);
            var embeddingArray = doc.RootElement
                .GetProperty("data")[0]
                .GetProperty("embedding")
                .EnumerateArray()
                .Select(e => e.GetSingle())
                .ToArray();

            return embeddingArray;
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Embedding generation failed");
            return null;
        }
    }

    // ─── Store embedding ──────────────────────────────────────────────────────

    public async Task StoreEmbeddingAsync(Guid tenantId, Guid projectId, string entityType, Guid entityId,
        string text, int importance = 3, bool isInstitutionalMemory = false)
    {
        var embedding = await GenerateEmbeddingAsync(text);
        if (embedding == null) return;

        // Update existing or insert new
        var existing = await _db.EmbeddingVectors
            .FirstOrDefaultAsync(e => e.EntityType == entityType && e.EntityId == entityId);

        if (existing != null)
        {
            existing.EmbeddedText = text;
            existing.EmbeddingJson = JsonSerializer.Serialize(embedding);
            existing.Importance = importance;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            _db.EmbeddingVectors.Add(new EmbeddingVector
            {
                TenantId = tenantId,
                ProjectId = projectId,
                EntityType = entityType,
                EntityId = entityId,
                EmbeddedText = text,
                EmbeddingJson = JsonSerializer.Serialize(embedding),
                Importance = importance,
                IsInstitutionalMemory = isInstitutionalMemory
            });
        }

        await _db.SaveChangesAsync();
    }

    // ─── Semantic search ──────────────────────────────────────────────────────

    public record SemanticMatch(
        string EntityType,
        Guid EntityId,
        string Text,
        double Score,
        int Importance);

    public async Task<List<SemanticMatch>> SearchAsync(
        Guid tenantId,
        string query,
        Guid? projectId = null,
        string[]? entityTypes = null,
        int topK = 8,
        bool includeInstitutionalMemory = false)
    {
        var queryEmbedding = await GenerateEmbeddingAsync(query);
        if (queryEmbedding == null) return [];

        // Load candidate vectors from DB (tenant-scoped)
        var dbQuery = _db.EmbeddingVectors.Where(e => e.TenantId == tenantId);

        if (projectId.HasValue)
            dbQuery = includeInstitutionalMemory
                ? dbQuery.Where(e => e.ProjectId == projectId.Value || e.IsInstitutionalMemory)
                : dbQuery.Where(e => e.ProjectId == projectId.Value);

        if (entityTypes?.Length > 0)
            dbQuery = dbQuery.Where(e => entityTypes.Contains(e.EntityType));

        var candidates = await dbQuery.ToListAsync();

        // Compute cosine similarity in-memory
        var scored = candidates
            .Select(c =>
            {
                float[]? vec = null;
                try { vec = JsonSerializer.Deserialize<float[]>(c.EmbeddingJson); } catch { }
                if (vec == null) return null;
                var score = CosineSimilarity(queryEmbedding, vec);
                return new SemanticMatch(c.EntityType, c.EntityId, c.EmbeddedText, score, c.Importance);
            })
            .Where(m => m != null)
            .Cast<SemanticMatch>()
            .OrderByDescending(m => m.Score * (1 + m.Importance * 0.05))  // boost by importance
            .Take(topK)
            .ToList();

        return scored;
    }

    // ─── Project matching via semantic similarity ─────────────────────────────

    public async Task<List<(Guid ProjectId, string ProjectName, double Score)>> MatchProjectAsync(
        Guid tenantId, string transcriptSummary, int topK = 3)
    {
        var embedding = await GenerateEmbeddingAsync(transcriptSummary);
        if (embedding == null) return [];

        var projectVectors = await _db.EmbeddingVectors
            .Where(e => e.TenantId == tenantId && e.EntityType == "project")
            .Include(e => e.Project)
            .ToListAsync();

        return projectVectors
            .Select(v =>
            {
                float[]? vec = null;
                try { vec = JsonSerializer.Deserialize<float[]>(v.EmbeddingJson); } catch { }
                if (vec == null || v.Project == null) return ((Guid, string, double)?)null;
                return (v.ProjectId, v.Project.Name, CosineSimilarity(embedding, vec));
            })
            .Where(x => x != null)
            .Cast<(Guid, string, double)>()
            .OrderByDescending(x => x.Item3)
            .Take(topK)
            .ToList();
    }

    // ─── Backfill all existing knowledge items ────────────────────────────────

    public async Task BackfillKnowledgeItemsAsync(Guid tenantId)
    {
        var items = await _db.ProjectKnowledgeItems
            .Where(k => k.Project!.TenantId == tenantId)
            .ToListAsync();

        foreach (var item in items)
        {
            var alreadyEmbedded = await _db.EmbeddingVectors
                .AnyAsync(e => e.EntityType == "knowledge" && e.EntityId == item.Id);

            if (!alreadyEmbedded)
            {
                var text = $"{item.Title}\n{item.Content}";
                await StoreEmbeddingAsync(tenantId, item.ProjectId, "knowledge", item.Id, text, item.Importance);
            }
        }
    }

    // ─── Cosine similarity ────────────────────────────────────────────────────

    private static double CosineSimilarity(float[] a, float[] b)
    {
        if (a.Length != b.Length) return 0;
        double dot = 0, normA = 0, normB = 0;
        for (var i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            normA += a[i] * a[i];
            normB += b[i] * b[i];
        }
        if (normA == 0 || normB == 0) return 0;
        return dot / (Math.Sqrt(normA) * Math.Sqrt(normB));
    }
}
