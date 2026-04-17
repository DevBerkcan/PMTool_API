using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace PmTool.Api.Hubs;

/// <summary>
/// Real-time SignalR hub for live updates:
/// - Task created/updated/deleted
/// - Meeting extracted
/// - AI notifications
/// - Monday briefings
///
/// Clients join tenant group on connect.
/// Frontend: import * as signalR from '@microsoft/signalr'
/// </summary>
[Authorize]
public class PmHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var tenantId = Context.User?.FindFirstValue("tenantId");
        if (!string.IsNullOrWhiteSpace(tenantId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, tenantId);
        }
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var tenantId = Context.User?.FindFirstValue("tenantId");
        if (!string.IsNullOrWhiteSpace(tenantId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, tenantId);
        }
        await base.OnDisconnectedAsync(exception);
    }

    // Client can subscribe to a specific project channel
    public async Task JoinProject(string projectId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"project-{projectId}");
    }

    public async Task LeaveProject(string projectId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"project-{projectId}");
    }
}

// ─── Hub event names (shared constants) ─────────────────────────────────────

public static class HubEvents
{
    // Task events
    public const string TaskCreated = "TaskCreated";
    public const string TaskUpdated = "TaskUpdated";
    public const string TaskDeleted = "TaskDeleted";

    // Meeting events
    public const string MeetingExtracted = "MeetingExtracted";
    public const string TranscriptReceived = "TranscriptReceived";

    // AI events
    public const string AiNotification = "AiNotification";
    public const string MondayBriefingReady = "MondayBriefingReady";
    public const string EmbeddingBackfillComplete = "EmbeddingBackfillComplete";

    // Webhook events
    public const string WebhookProcessed = "WebhookProcessed";
}
