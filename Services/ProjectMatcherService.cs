using Microsoft.EntityFrameworkCore;
using PmTool.Api.Models;

namespace PmTool.Api.Services;

/// <summary>
/// Three-layer project matching for auto-assigning meeting transcripts to projects:
///   Layer 1 — Exact: TeamsChannelId → ProjectTeamsLink
///   Layer 2 — Semantic: Cosine similarity of transcript embedding vs project embeddings
///   Layer 3 — Participant: Match attendee emails → ResourceAllocation → Project
/// </summary>
public class ProjectMatcherService
{
    private readonly AppDbContext _db;
    private readonly EmbeddingService _embeddings;
    private readonly ILogger<ProjectMatcherService> _log;

    public ProjectMatcherService(AppDbContext db, EmbeddingService embeddings, ILogger<ProjectMatcherService> log)
    {
        _db = db;
        _embeddings = embeddings;
        _log = log;
    }

    public record MatchResult(
        Guid? ProjectId,
        string ProjectName,
        string MatchMethod,   // exact | semantic | participant | none
        double Confidence,    // 0–1
        bool RequiresUserConfirmation);

    /// <summary>
    /// Match a transcript to a project using all three layers.
    /// </summary>
    public async Task<MatchResult> MatchAsync(
        Guid tenantId,
        string? teamsChannelId,
        string transcriptText,
        List<string>? participantEmails = null)
    {
        // Layer 1 — Exact Teams channel match
        if (!string.IsNullOrWhiteSpace(teamsChannelId))
        {
            var teamsLink = await _db.ProjectTeamsLinks
                .Include(t => t.Project)
                .FirstOrDefaultAsync(t => t.ChannelId == teamsChannelId && t.Project!.TenantId == tenantId);

            if (teamsLink?.Project != null)
            {
                _log.LogInformation("Project matched via Teams channel: {Project}", teamsLink.Project.Name);
                return new MatchResult(teamsLink.ProjectId, teamsLink.Project.Name, "exact", 1.0, false);
            }
        }

        // Layer 2 — Semantic similarity
        var summary = transcriptText.Length > 800 ? transcriptText[..800] : transcriptText;
        var semanticMatches = await _embeddings.MatchProjectAsync(tenantId, summary, topK: 3);

        if (semanticMatches.Count > 0)
        {
            var best = semanticMatches[0];

            if (best.Score >= 0.80)
            {
                _log.LogInformation("Project matched via semantics (score {Score:F2}): {Project}", best.Score, best.ProjectName);
                return new MatchResult(best.ProjectId, best.ProjectName, "semantic", best.Score, false);
            }

            if (best.Score >= 0.60)
            {
                _log.LogInformation("Low-confidence semantic match (score {Score:F2}): {Project}", best.Score, best.ProjectName);
                return new MatchResult(best.ProjectId, best.ProjectName, "semantic", best.Score, true);
            }
        }

        // Layer 3 — Participant email matching
        if (participantEmails?.Count > 0)
        {
            var lowerEmails = participantEmails.Select(e => e.ToLowerInvariant()).ToList();

            var allocations = await _db.ResourceAllocations
                .Include(r => r.User)
                .Include(r => r.Project)
                .Where(r => r.Project!.TenantId == tenantId && lowerEmails.Contains(r.User!.Email.ToLower()))
                .ToListAsync();

            var projectCounts = allocations
                .GroupBy(a => new { a.ProjectId, Name = a.Project?.Name ?? "" })
                .OrderByDescending(g => g.Count())
                .FirstOrDefault();

            if (projectCounts != null && projectCounts.Count() >= 2)
            {
                var confidence = Math.Min(0.75, projectCounts.Count() * 0.15);
                _log.LogInformation("Project matched via participants ({Count} matches): {Project}", projectCounts.Count(), projectCounts.Key.Name);
                return new MatchResult(projectCounts.Key.ProjectId, projectCounts.Key.Name, "participant", confidence, true);
            }
        }

        _log.LogInformation("No project match found for transcript (tenant {TenantId})", tenantId);
        return new MatchResult(null, "", "none", 0, true);
    }

    /// <summary>
    /// Embed all project summaries so they can be matched against transcripts.
    /// Call after project creation/update.
    /// </summary>
    public async Task EmbedProjectSummaryAsync(Guid tenantId, Guid projectId)
    {
        var project = await _db.Projects.FindAsync(projectId);
        if (project == null || project.TenantId != tenantId) return;

        var text = $"{project.Name}\n{project.Description}\n{project.Objective}\n{project.Scope}\n{project.StakeholdersCsv}\n{project.TechnologiesCsv}";
        await _embeddings.StoreEmbeddingAsync(tenantId, projectId, "project", projectId, text, 5);
    }

    /// <summary>
    /// Backfill project embeddings for all projects in a tenant.
    /// </summary>
    public async Task BackfillProjectEmbeddingsAsync(Guid tenantId)
    {
        var projects = await _db.Projects.Where(p => p.TenantId == tenantId).ToListAsync();
        foreach (var p in projects)
            await EmbedProjectSummaryAsync(tenantId, p.Id);
    }
}
