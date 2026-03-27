using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PmTool.Api.Models;
using System.Security.Claims;

namespace PmTool.Api.Controllers;

[ApiController, Route("api/v1/[controller]"), Authorize]
public class GovernanceController : ControllerBase
{
    private readonly AppDbContext _db;
    private Guid TenantId => Guid.Parse(User.FindFirstValue("tenantId")!);

    public GovernanceController(AppDbContext db) => _db = db;

    [HttpGet("overview")]
    public async Task<ActionResult<GovernanceOverviewDto>> GetOverview()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var projects = await _db.Projects
            .Where(p => p.TenantId == TenantId)
            .Include(p => p.Decisions)
            .Include(p => p.Milestones)
            .Include(p => p.GovernanceChecks)
            .Include(p => p.StageGates)
            .Include(p => p.Approvals)
            .OrderBy(p => p.Name)
            .ToListAsync();

        var items = projects.Select(project => new GovernanceOverviewProjectDto(
            project.Id,
            project.Name,
            project.Category,
            project.Stage,
            project.Status,
            project.GovernanceChecks.Count(check => check.Status != "done"),
            project.Decisions.Count(decision => decision.Status == "open" || decision.Status == "review"),
            project.Milestones.Count(milestone => milestone.DueDate < today && milestone.Status != "done"),
            project.StageGates.Count(gate => gate.Status != "approved"),
            project.StageGates.Count(gate => gate.Status == "blocked"),
            project.Approvals.Count(approval => approval.Status == "pending")
        )).ToList();

        return Ok(new GovernanceOverviewDto(
            items,
            items.Count,
            items.Sum(item => item.OpenGovernanceChecks),
            items.Sum(item => item.OpenDecisions),
            items.Sum(item => item.OverdueMilestones),
            items.Sum(item => item.OpenStageGates),
            items.Sum(item => item.BlockedStageGates),
            items.Sum(item => item.PendingApprovals)
        ));
    }

    [HttpGet("escalations")]
    public async Task<ActionResult<PortfolioEscalationOverviewDto>> GetEscalations()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var projects = await _db.Projects
            .Where(project => project.TenantId == TenantId)
            .Include(project => project.StageGates)
            .Include(project => project.Approvals)
            .Include(project => project.Tasks)
            .Include(project => project.TeamAssignments)
            .OrderBy(project => project.Name)
            .ToListAsync();

        var capacityByUser = await _db.ResourceAllocations
            .Where(allocation => allocation.Project!.TenantId == TenantId)
            .Include(allocation => allocation.User)
            .Include(allocation => allocation.Project)
            .GroupBy(allocation => new { allocation.UserId, UserName = allocation.User!.DisplayName })
            .Select(group => new
            {
                group.Key.UserId,
                group.Key.UserName,
                TotalHours = group.Sum(item => item.AllocatedHours)
            })
            .ToListAsync();

        var capacityLookup = capacityByUser.ToDictionary(item => item.UserId, item => item);
        var items = new List<PortfolioEscalationItemDto>();

        foreach (var project in projects)
        {
            var budgetPercent = project.BudgetTotal <= 0 ? 0 : (project.BudgetSpent / project.BudgetTotal) * 100;
            if (budgetPercent >= 85)
            {
                var severity = budgetPercent >= 100 ? "critical" : "warning";
                items.Add(new PortfolioEscalationItemDto(
                    project.Id,
                    project.Name,
                    severity,
                    "budget",
                    severity == "critical" ? "Budget ueberschritten" : "Budgetwarnung",
                    $"{budgetPercent:0}% des Projektbudgets sind verbraucht.",
                    $"Budget {project.BudgetSpent:0}/{project.BudgetTotal:0}",
                    "Scope, Forecast und Restaufwand mit Projektleitung und Management neu validieren."
                ));
            }

            foreach (var allocation in project.TeamAssignments)
            {
                if (!capacityLookup.TryGetValue(allocation.UserId, out var load))
                {
                    continue;
                }

                if (load.TotalHours >= 36)
                {
                    items.Add(new PortfolioEscalationItemDto(
                        project.Id,
                        project.Name,
                        load.TotalHours > 40 ? "critical" : "warning",
                        "capacity",
                        $"{load.UserName} ist kapazitiv am Limit",
                        $"{load.UserName} ist mit {load.TotalHours}h ueber alle Projekte hinweg eingeplant.",
                        $"Kapazitaet {load.TotalHours}/40h",
                        "Prioritaeten zwischen Projekten klaeren und Auslastung neu schneiden."
                    ));
                }
            }

            var blockedGates = project.StageGates.Count(gate => gate.Status == "blocked");
            if (blockedGates > 0)
            {
                items.Add(new PortfolioEscalationItemDto(
                    project.Id,
                    project.Name,
                    "critical",
                    "stage_gate",
                    "Blockiertes Stage Gate",
                    $"{blockedGates} Stage Gates sind blockiert und verhindern den Uebergang in die naechste Phase.",
                    $"{blockedGates} Gates blockiert",
                    "Pflichtartefakte, offene Freigaben und Blocker sofort im PMO Review aufloesen."
                ));
            }

            var overdueApprovals = project.Approvals.Count(approval => approval.Status == "pending" && approval.DueDate < today);
            if (overdueApprovals > 0)
            {
                items.Add(new PortfolioEscalationItemDto(
                    project.Id,
                    project.Name,
                    "warning",
                    "approval",
                    "Ueberfaellige Freigaben",
                    $"{overdueApprovals} Freigaben sind ueberfaellig und verzoegern die Projektsteuerung.",
                    $"{overdueApprovals} Freigaben offen",
                    "Entscheider benennen und Freigabe im naechsten Steering fest terminieren."
                ));
            }
        }

        var ordered = items
            .OrderByDescending(item => item.Severity == "critical")
            .ThenBy(item => item.ProjectName)
            .ThenBy(item => item.Category)
            .ToList();

        return Ok(new PortfolioEscalationOverviewDto(
            ordered.Count,
            ordered.Count(item => item.Severity == "critical"),
            ordered.Count(item => item.Severity != "critical"),
            ordered.Count(item => item.Category == "budget"),
            ordered.Count(item => item.Category == "capacity"),
            ordered
        ));
    }
}
