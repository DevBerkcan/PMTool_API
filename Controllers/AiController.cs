using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PmTool.Api.Models;
using System.Security.Claims;

namespace PmTool.Api.Controllers;

[ApiController, Route("api/v1/[controller]"), Authorize]
public class AiController : ControllerBase
{
    private readonly AppDbContext _db;
    private Guid TenantId => Guid.Parse(User.FindFirstValue("tenantId")!);
    public AiController(AppDbContext db) => _db = db;

    [HttpPost("chat")]
    public async Task<ActionResult<AiChatResponse>> Chat([FromBody] AiChatRequest req)
    {
        var projects = await _db.Projects.Where(p => p.TenantId == TenantId).Include(p => p.Tasks).Include(p => p.Risks).ToListAsync();
        var q = req.Message.ToLower();
        string reply;

        if (q.Contains("status") || q.Contains("ueberblick"))
        {
            var red = projects.Where(p => p.Status == "red").ToList();
            var yellow = projects.Where(p => p.Status == "yellow").ToList();
            reply = $"Portfolio: {projects.Count} Projekte | Gruen: {projects.Count(p => p.Status == "green")} | Gelb: {yellow.Count} ({string.Join(", ", yellow.Select(p => p.Name))}) | Rot: {red.Count} ({string.Join(", ", red.Select(p => p.Name))})";
        }
        else if (q.Contains("risik"))
        {
            var allRisks = projects.SelectMany(p => p.Risks).ToList();
            var critical = allRisks.Where(r => r.Impact * r.Probability >= 15).ToList();
            reply = $"Risiken: {allRisks.Count} gesamt, {critical.Count} kritisch. " + string.Join(" | ", critical.Select(r => $"{r.Title} (Score {r.Impact * r.Probability})"));
        }
        else if (q.Contains("task"))
        {
            var allTasks = projects.SelectMany(p => p.Tasks).ToList();
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            reply = $"Tasks: {allTasks.Count} gesamt | Ueberfaellig: {allTasks.Count(t => t.DueDate < today && t.Status != "done")} | In Progress: {allTasks.Count(t => t.Status == "in_progress")}";
        }
        else
        {
            reply = "Ich helfe bei Projektstatus, Risiken und Tasks. Stellen Sie eine konkrete Frage!";
        }

        return Ok(new AiChatResponse(reply));
    }
}
