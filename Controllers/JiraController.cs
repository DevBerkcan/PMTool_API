using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PmTool.Api.Models;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace PmTool.Api.Controllers;

[ApiController, Route("api/v1/[controller]"), Authorize]
public class JiraController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;

    private Guid TenantId => Guid.Parse(User.FindFirstValue("tenantId")!);

    public JiraController(AppDbContext db, IConfiguration configuration, IHttpClientFactory httpClientFactory)
    {
        _db = db;
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
    }

    [HttpGet("projects/{projectId}/tickets")]
    public async Task<ActionResult<JiraProjectTicketsDto>> GetProjectTickets(Guid projectId, [FromQuery] int maxResults = 50)
    {
        var project = await _db.Projects
            .Include(p => p.JiraLink)
            .FirstOrDefaultAsync(p => p.Id == projectId && p.TenantId == TenantId);

        if (project == null) return NotFound();
        if (project.JiraLink == null) return BadRequest(new { message = "Fuer dieses Projekt ist noch kein Jira-Link hinterlegt." });

        var baseUrl = (_configuration["Jira:BaseUrl"] ?? "").Trim().TrimEnd('/');
        var authMode = (_configuration["Jira:AuthMode"] ?? "basic").Trim().ToLowerInvariant();
        var email = (_configuration["Jira:Email"] ?? "").Trim();
        var apiToken = (_configuration["Jira:ApiToken"] ?? "").Trim();

        if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(apiToken) || (authMode != "bearer" && string.IsNullOrWhiteSpace(email)))
        {
            return BadRequest(new { message = "Jira ist noch nicht konfiguriert. BaseUrl, ApiToken und ggf. Email setzen." });
        }

        var jql = BuildJql(project.JiraLink);
        var requestBody = new
        {
            jql,
            maxResults = Math.Clamp(maxResults, 1, 100),
            fields = new[] { "summary", "status", "assignee", "priority", "issuetype", "updated", "duedate" }
        };

        var client = _httpClientFactory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/rest/api/3/search")
        {
            Content = JsonContent.Create(requestBody)
        };

        if (authMode == "bearer")
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiToken);
        }
        else
        {
            var authValue = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{email}:{apiToken}"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authValue);
        }

        using var response = await client.SendAsync(request);
        var responseText = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            return BadRequest(new { message = $"Jira Anfrage fehlgeschlagen: {responseText}" });
        }

        using var document = JsonDocument.Parse(responseText);
        var issues = document.RootElement.TryGetProperty("issues", out var issuesElement)
            ? issuesElement.EnumerateArray().Select(issue => ParseIssue(issue, baseUrl)).ToList()
            : [];

        project.JiraLink.LastSyncAt = DateTime.UtcNow;
        project.JiraLink.UpdatedAt = DateTime.UtcNow;
        if (project.JiraLink.SyncStatus == "planned")
        {
            project.JiraLink.SyncStatus = "connected";
        }

        await _db.SaveChangesAsync();

        var grouped = issues
            .GroupBy(issue => new { issue.AssigneeName, issue.AssigneeEmail })
            .Select(group =>
            {
                var tickets = group.OrderByDescending(ticket => ticket.UpdatedAt).ToList();
                return new JiraAssigneeTicketsDto(
                    string.IsNullOrWhiteSpace(group.Key.AssigneeName) ? "Nicht zugewiesen" : group.Key.AssigneeName,
                    group.Key.AssigneeEmail,
                    tickets.Count,
                    tickets.Count(ticket => IsTodoStatus(ticket.StatusCategory, ticket.Status)),
                    tickets.Count(ticket => IsInProgressStatus(ticket.StatusCategory, ticket.Status)),
                    tickets.Count(ticket => IsDoneStatus(ticket.StatusCategory, ticket.Status)),
                    tickets
                );
            })
            .OrderByDescending(group => group.InProgressTickets)
            .ThenByDescending(group => group.TotalTickets)
            .ThenBy(group => group.AssigneeName)
            .ToList();

        return Ok(new JiraProjectTicketsDto(
            project.Id,
            project.Name,
            project.JiraLink.BoardName,
            project.JiraLink.ProjectKey,
            jql,
            issues.Count,
            issues.Count(issue => string.IsNullOrWhiteSpace(issue.AssigneeName)),
            grouped,
            issues.OrderByDescending(issue => issue.UpdatedAt).ToList()
        ));
    }

    private static string BuildJql(ProjectJiraLink link)
    {
        if (!string.IsNullOrWhiteSpace(link.JqlFilter))
        {
            return link.JqlFilter;
        }

        if (!string.IsNullOrWhiteSpace(link.ProjectKey))
        {
            return $"project = {link.ProjectKey} ORDER BY updated DESC";
        }

        return "ORDER BY updated DESC";
    }

    private static JiraTicketDto ParseIssue(JsonElement issue, string baseUrl)
    {
        var key = GetString(issue, "key");
        var fields = issue.TryGetProperty("fields", out var fieldsElement) ? fieldsElement : default;

        return new JiraTicketDto(
            key,
            GetString(fields, "summary"),
            GetNestedString(fields, "status", "name"),
            GetNestedString(fields, "status", "statusCategory", "key"),
            GetNestedString(fields, "priority", "name"),
            GetNestedString(fields, "issuetype", "name"),
            GetNestedString(fields, "assignee", "displayName"),
            GetNestedString(fields, "assignee", "emailAddress"),
            string.IsNullOrWhiteSpace(key) ? "" : $"{baseUrl}/browse/{key}",
            GetDateTime(fields, "updated"),
            GetDateOnly(fields, "duedate")
        );
    }

    private static bool IsDoneStatus(string statusCategory, string status) =>
        statusCategory.Equals("done", StringComparison.OrdinalIgnoreCase)
        || status.Contains("done", StringComparison.OrdinalIgnoreCase)
        || status.Contains("closed", StringComparison.OrdinalIgnoreCase)
        || status.Contains("resolved", StringComparison.OrdinalIgnoreCase);

    private static bool IsInProgressStatus(string statusCategory, string status) =>
        statusCategory.Equals("indeterminate", StringComparison.OrdinalIgnoreCase)
        || status.Contains("progress", StringComparison.OrdinalIgnoreCase)
        || status.Contains("review", StringComparison.OrdinalIgnoreCase)
        || status.Contains("testing", StringComparison.OrdinalIgnoreCase);

    private static bool IsTodoStatus(string statusCategory, string status) =>
        !IsDoneStatus(statusCategory, status) && !IsInProgressStatus(statusCategory, status);

    private static string GetString(JsonElement element, string propertyName)
    {
        if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(propertyName, out var property))
        {
            return "";
        }

        return property.ValueKind == JsonValueKind.String ? property.GetString() ?? "" : "";
    }

    private static string GetNestedString(JsonElement element, params string[] path)
    {
        var current = element;
        foreach (var segment in path)
        {
            if (current.ValueKind != JsonValueKind.Object || !current.TryGetProperty(segment, out current))
            {
                return "";
            }
        }

        return current.ValueKind == JsonValueKind.String ? current.GetString() ?? "" : "";
    }

    private static DateTime? GetDateTime(JsonElement element, string propertyName)
    {
        var raw = GetString(element, propertyName);
        return DateTime.TryParse(raw, out var value) ? value : null;
    }

    private static DateOnly? GetDateOnly(JsonElement element, string propertyName)
    {
        var raw = GetString(element, propertyName);
        return DateOnly.TryParse(raw, out var value) ? value : null;
    }
}
