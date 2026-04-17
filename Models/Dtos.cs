namespace PmTool.Api.Models;

public record LoginRequest(string Email, string Password);
public record LoginResponse(string Token, string UserName, string Email, string Role, Guid UserId, Guid TenantId);
public record EntraExchangeRequest(string IdToken);
public record AccessMatrixDto(string CurrentRole, List<string> AvailableRoles, Dictionary<string, bool> Permissions);
public record EntraRoleMappingDto(string Role, string GroupId, bool IsConfigured);
public record EntraIntegrationStatusDto(bool IsConfigured, string TenantId, string ClientId, bool AutoProvisionUsers, string DefaultRole, List<string> AllowedDomains, List<EntraRoleMappingDto> RoleMappings, string SetupHint);

public record ProjectDto(
    Guid Id,
    string Name,
    string Description,
    string Customer,
    string Category,
    string Stage,
    string Status,
    int ProgressPercent,
    decimal BudgetTotal,
    decimal BudgetSpent,
    DateOnly StartDate,
    DateOnly EndDate,
    int TeamSize,
    string OwnerName,
    DateTime CreatedAt
);

public record ProjectTeamMemberDto(
    Guid UserId,
    string Name,
    string Email,
    string Role,
    string ProjectRole,
    string Responsibility,
    int AllocatedHours,
    int TotalCapacityHours
);

public record ProjectNoteDto(Guid Id, string Title, string Content, string AuthorName, DateTime CreatedAt, string Category, string Participants, DateOnly? MeetingDate, bool IsPinned);
public record ProjectContactDto(Guid Id, string Name, string Email, string Phone, string Company, string Role, string Supervisor, string Notes);
public record ProjectLeadTaskDto(Guid Id, string Title, string Description, string OwnerName, DateOnly DueDate, string Status);
public record ProjectMilestoneDto(Guid Id, string Title, string Description, string OwnerName, DateOnly DueDate, string Status);
public record ProjectDecisionDto(Guid Id, string Title, string Context, string Decision, string OwnerName, DateOnly DueDate, string Status);
public record ProjectDocumentDto(Guid Id, string Title, string Category, string Url, string Status, string OwnerName, DateTime CreatedAt);
public record ProjectGovernanceCheckDto(Guid Id, string Title, string Area, string Notes, string OwnerName, DateOnly DueDate, string Status);
public record ProjectStageGateCheckDto(Guid Id, string Title, string RequirementType, string Status, bool IsMandatory, string Notes);
public record ProjectStageGateDto(Guid Id, string Title, string StageKey, int GateOrder, string Status, DateOnly DueDate, string OwnerName, string Notes, string ApprovalSummary, List<ProjectStageGateCheckDto> Checks);
public record ProjectApprovalDto(Guid Id, Guid? StageGateId, string Title, string ApprovalType, string Status, DateOnly DueDate, string RequestedByName, string DecidedByName, string DecisionNotes);
public record ProjectForecastDto(Guid ProjectId, string ProjectName, decimal BudgetAtCompletion, decimal ActualCost, decimal EarnedValue, decimal PlannedValue, decimal CostVariance, decimal ScheduleVariance, decimal EstimateAtCompletion, decimal EstimateToComplete, decimal CostPerformanceIndex, decimal SchedulePerformanceIndex, int TotalEstimatedHours, int LoggedHours, int RemainingHours, string ForecastComment);
public record ProjectForecastSnapshotDto(Guid Id, DateOnly SnapshotDate, decimal BudgetAtCompletion, decimal ActualCost, decimal EarnedValue, decimal PlannedValue, decimal EstimateAtCompletion, decimal EstimateToComplete, decimal CostPerformanceIndex, decimal SchedulePerformanceIndex, int RemainingHours);
public record ProjectKnowledgeItemDto(Guid Id, string Title, string SourceType, string SourceLabel, string Category, string SourceFileName, int Version, Guid? ParentKnowledgeItemId, string LinkedEntityType, Guid? LinkedEntityId, string MeetingReference, string Content, List<string> Tags, string AuthorName, int Importance, DateTime CreatedAt);
public record KnowledgeSourceStatDto(string Key, int Count, int HighImportanceCount);
public record KnowledgeTagStatDto(string Tag, int Count);
public record KnowledgeChunkDto(Guid KnowledgeItemId, string KnowledgeTitle, string SourceType, string Category, int ChunkIndex, string Text, int SemanticScore);
public record ProjectKnowledgeHubItemDto(Guid Id, string Title, string SourceType, string SourceLabel, string Category, string SourceFileName, int Version, Guid? ParentKnowledgeItemId, string LinkedEntityType, Guid? LinkedEntityId, string MeetingReference, string Content, string Excerpt, List<string> Tags, string AuthorName, int Importance, DateTime CreatedAt, int RelevanceScore);
public record ProjectKnowledgeHubDto(Guid ProjectId, string ProjectName, int TotalItems, int HighImportanceItems, List<KnowledgeSourceStatDto> Sources, List<KnowledgeTagStatDto> TopTags, List<ProjectKnowledgeHubItemDto> Items, List<KnowledgeChunkDto> SemanticMatches);
public record ProjectTeamsLinkDto(Guid ProjectId, string TeamName, string ChannelName, string TeamId, string ChannelId, string TenantDomain, string SyncStatus, DateTime? LastSyncAt);
public record ProjectJiraLinkDto(Guid ProjectId, string BoardName, string ProjectKey, string BoardId, string JqlFilter, string SyncStatus, DateTime? LastSyncAt);

public record ProjectDetailDto(
    Guid Id,
    string Name,
    string Description,
    string Customer,
    string Category,
    string Stage,
    string DeliveryModel,
    string Sponsor,
    string ExecutiveSummary,
    string HealthSummary,
    string Objective,
    string Scope,
    string SuccessMetric,
    string Communication,
    string NextMilestone,
    List<string> Stakeholders,
    List<string> Technologies,
    string Status,
    int ProgressPercent,
    decimal BudgetTotal,
    decimal BudgetSpent,
    DateOnly StartDate,
    DateOnly EndDate,
    string OwnerName,
    DateTime CreatedAt,
    List<ProjectTeamMemberDto> TeamMembers,
    List<ProjectNoteDto> Notes,
    List<ProjectLeadTaskDto> LeadTasks,
    List<ProjectMilestoneDto> Milestones,
    List<ProjectDecisionDto> Decisions,
    List<ProjectDocumentDto> Documents,
    List<ProjectGovernanceCheckDto> GovernanceChecks,
    List<ProjectStageGateDto> StageGates,
    List<ProjectApprovalDto> Approvals,
    List<ProjectKnowledgeItemDto> KnowledgeItems,
    ProjectTeamsLinkDto? TeamsLink,
    ProjectJiraLinkDto? JiraLink,
    List<ProjectContactDto> Contacts
);

public record CreateProjectRequest(string Name, string Description, string Customer, decimal BudgetTotal, DateOnly StartDate, DateOnly EndDate);
public record UpdateProjectRequest(string? Name, string? Description, string? Customer, string? Category, string? Stage, string? DeliveryModel, string? Sponsor, string? ExecutiveSummary, string? HealthSummary, string? Objective, string? Scope, string? SuccessMetric, string? Communication, string? NextMilestone, List<string>? Stakeholders, List<string>? Technologies, string? Status, int? ProgressPercent, decimal? BudgetTotal, decimal? BudgetSpent, DateOnly? StartDate, DateOnly? EndDate);
public record PortfolioDto(List<ProjectDto> Projects, int TotalProjects, int GreenCount, int YellowCount, int RedCount, decimal TotalBudget, decimal SpentBudget, decimal ForecastBudget, decimal ForecastVariance, int TotalTasks, int OverdueTasks, int OpenDecisions, int OverdueMilestones, int OpenGovernanceItems, int OverloadedMembers, int NearCapacityMembers);

public record AssignProjectTeamMemberRequest(Guid UserId, string ProjectRole, string Responsibility, int AllocatedHours);
public record UpdateProjectTeamMemberRequest(string? ProjectRole, string? Responsibility, int? AllocatedHours);

public record TaskDto(Guid Id, Guid ProjectId, string Title, string Description, string Status, string Priority, Guid? AssigneeId, string? AssigneeName, DateOnly? DueDate, int EstimatedHours, int LoggedHours, int CommentCount, DateTime CreatedAt);
public record CreateTaskRequest(string Title, string? Description, string Priority, Guid? AssigneeId, DateOnly? DueDate, int EstimatedHours);
public record UpdateTaskStatusRequest(string Status);
public record CommentDto(Guid Id, string Content, string AuthorName, DateTime CreatedAt);
public record AddCommentRequest(string Content);

public record RiskDto(Guid Id, Guid ProjectId, string Title, string Description, int Impact, int Probability, int Score, string Status, string Mitigation, string OwnerName, DateTime IdentifiedAt);
public record CreateRiskRequest(string Title, string Description, int Impact, int Probability, string Mitigation, Guid OwnerId);

public record ActivityDto(Guid Id, string UserName, string Action, string EntityType, DateTime CreatedAt);
public record AuditEntryDto(Guid Id, Guid? ProjectId, string UserName, string UserRole, string EntityType, string ChangeType, string Title, string FromValue, string ToValue, string Detail, DateTime CreatedAt);
public record TeamMemberDto(Guid Id, string Name, string Email, string Role, int AllocatedHours, int TotalCapacityHours);
public record InviteTeamMemberRequest(string Name, string Email, string Role);
public record UpdateTeamMemberRequest(string? Name, string? Role);

public record CreateProjectNoteRequest(string Title, string Content, string? Category, string? Participants, DateOnly? MeetingDate);
public record UpdateProjectNoteRequest(string Title, string Content, string? Category, string? Participants, DateOnly? MeetingDate, bool? IsPinned);
public record UpsertProjectContactRequest(string Name, string? Email, string? Phone, string? Company, string? Role, string? Supervisor, string? Notes);
public record CreateProjectLeadTaskRequest(string Title, string Description, Guid? OwnerId, DateOnly DueDate);
public record UpdateProjectLeadTaskStatusRequest(string Status);
public record CreateProjectMilestoneRequest(string Title, string Description, Guid? OwnerId, DateOnly DueDate);
public record UpdateProjectMilestoneStatusRequest(string Status);
public record CreateProjectDecisionRequest(string Title, string Context, string Decision, Guid? OwnerId, DateOnly DueDate);
public record UpdateProjectDecisionStatusRequest(string Status);
public record CreateProjectDocumentRequest(string Title, string Category, string Url, string Status, Guid? OwnerId);
public record CreateProjectGovernanceCheckRequest(string Title, string Area, string Notes, Guid? OwnerId, DateOnly DueDate);
public record UpdateProjectGovernanceCheckStatusRequest(string Status);
public record CreateProjectStageGateRequest(string Title, string StageKey, int? GateOrder, Guid? OwnerId, DateOnly DueDate, string Notes, string ApprovalSummary);
public record UpdateProjectStageGateStatusRequest(string Status);
public record CreateProjectStageGateCheckRequest(string Title, string RequirementType, bool? IsMandatory, string Notes);
public record UpdateProjectStageGateCheckStatusRequest(string Status);
public record CreateProjectApprovalRequest(Guid? StageGateId, string Title, string ApprovalType, DateOnly DueDate, string DecisionNotes);
public record UpdateProjectApprovalStatusRequest(string Status, string DecisionNotes);
public record CreateProjectKnowledgeItemRequest(string Title, string SourceType, string SourceLabel, string Category, string SourceFileName, Guid? ParentKnowledgeItemId, string LinkedEntityType, Guid? LinkedEntityId, string MeetingReference, string Content, List<string>? Tags, int? Importance);
public record UploadKnowledgeDocumentRequest(string Title, string SourceType, string SourceLabel, string Category, string SourceFileName, string Content, Guid? ParentKnowledgeItemId, string LinkedEntityType, Guid? LinkedEntityId, string MeetingReference, List<string>? Tags, int? Importance);
public record UpsertProjectTeamsLinkRequest(string TeamName, string ChannelName, string TeamId, string ChannelId, string TenantDomain, string SyncStatus);
public record UpsertProjectJiraLinkRequest(string BoardName, string ProjectKey, string BoardId, string JqlFilter, string SyncStatus);
public record GovernanceOverviewProjectDto(Guid ProjectId, string ProjectName, string Category, string Stage, string Status, int OpenGovernanceChecks, int OpenDecisions, int OverdueMilestones, int OpenStageGates, int BlockedStageGates, int PendingApprovals);
public record GovernanceOverviewDto(List<GovernanceOverviewProjectDto> Projects, int TotalProjects, int OpenGovernanceChecks, int OpenDecisions, int OverdueMilestones, int OpenStageGates, int BlockedStageGates, int PendingApprovals);
public record PortfolioEscalationItemDto(Guid ProjectId, string ProjectName, string Severity, string Category, string Title, string Detail, string Metric, string RecommendedAction);
public record PortfolioEscalationOverviewDto(int TotalItems, int CriticalItems, int WarningItems, int BudgetWarnings, int CapacityWarnings, List<PortfolioEscalationItemDto> Items);

public record AiChatRequest(string Message, Guid? ProjectId, string? ConversationId = null);
public record AiChatResponse(string Reply, string ConversationId, List<AiAnswerSourceDto> Sources);
public record ProjectAiQuestionRequest(Guid ProjectId, string Question);
public record AiAnswerSourceDto(string Type, string Title, string Detail, int RelevanceScore);
public record ProjectAiAnswerDto(Guid ProjectId, string ProjectName, string Question, string Answer, string Confidence, List<AiAnswerSourceDto> Sources, List<string> SuggestedActions);
public record AiSuggestionDto(Guid ProjectId, string ProjectName, string Type, string Title, string Reason, string Recommendation, string Priority, List<string> Sources, string FeedbackStatus);
public record AiSuggestionFeedbackDto(Guid Id, Guid ProjectId, string SuggestionType, string SuggestionTitle, string Status, string Notes, string UserName, DateTime CreatedAt);
public record PortfolioBriefingProjectDto(Guid ProjectId, string ProjectName, string Status, int ProgressPercent, int OpenTasks, int CriticalRisks, int OpenDecisions, int OpenGovernanceChecks, string Headline);
public record PortfolioBriefingDto(string Summary, List<string> Highlights, List<string> Escalations, List<PortfolioBriefingProjectDto> Projects);
public record RiskSignalDto(Guid ProjectId, string ProjectName, string Severity, string Title, string Detail, List<string> Sources, int Score);
public record AiLearningByTypeDto(string SuggestionType, int Accepted, int Rejected, int Edited);
public record AiLearningByProjectDto(Guid ProjectId, string ProjectName, int Accepted, int Rejected, int Edited);
public record AiLearningSummaryDto(int TotalFeedback, int Accepted, int Rejected, int Edited, List<AiLearningByTypeDto> ByType, List<AiLearningByProjectDto> ByProject, List<string> Insights);
public record CreateAiSuggestionFeedbackRequest(Guid ProjectId, string Type, string Title, string Status, string Notes);
public record ApplyAiSuggestionRequest(Guid ProjectId, string Type, string Title, string Recommendation, string TargetType, string? Notes);
public record ApplyAiSuggestionResponse(Guid ProjectId, string TargetType, Guid EntityId, string EntityTitle, string FeedbackStatus);
public record WeeklyStatusDto(Guid ProjectId, string ProjectName, string Summary, string DeliveryFocus, string RiskFocus, string GovernanceFocus, List<string> Highlights, List<string> NextActions);
public record ImportAnalyzeRequest(Guid ProjectId, string SourceType, string Content);
public record ImportPreviewRowDto(int RowNumber, List<string> Values);
public record ImportAnalyzeResponse(Guid ProjectId, string SourceType, int RowCount, int ColumnCount, List<string> Headers, List<ImportPreviewRowDto> Rows, string Summary);
public record ImportCommitRequest(Guid ProjectId, string SourceType, string Title, string Content);
public record ImportCommitResponse(Guid ProjectId, string Title, string SourceType, int ImportedRows, string Summary);
public record MeetingAnalyzeRequest(Guid ProjectId, string SourceType, string Title, string Content);
public record MeetingExtractedItemDto(string Type, string Title, string Detail);
public record MeetingAnalyzeResponse(Guid ProjectId, string SourceType, string Title, string Summary, List<MeetingExtractedItemDto> Actions, List<MeetingExtractedItemDto> Decisions, List<MeetingExtractedItemDto> Risks, List<MeetingExtractedItemDto> Knowledge);
public record MeetingCommitRequest(Guid ProjectId, string SourceType, string Title, string Content);
public record MeetingCommitResponse(Guid ProjectId, string Title, int CreatedTasks, int CreatedDecisions, int CreatedRisks, int CreatedKnowledgeItems, string Summary);
public record ProjectMeetingDto(Guid Id, string Title, DateTime MeetingDate, string Participants, string Location, string TeamsJoinUrl, string TeamsOnlineMeetingId, string TranscriptSource, DateTime? TranscriptFetchedAt, string ExtractionStatus, string Notes, string CreatedByName, DateTime CreatedAt, bool HasTranscript);
public record CreateProjectMeetingRequest(string Title, DateTime MeetingDate, string? Participants, string? Location, string? TeamsJoinUrl, string? TeamsOnlineMeetingId, string? Notes);
public record UpdateProjectMeetingRequest(string? Title, DateTime? MeetingDate, string? Participants, string? Location, string? TeamsJoinUrl, string? TeamsOnlineMeetingId, string? Notes);
public record AddTranscriptRequest(string TranscriptText);
public record GraphIntegrationStatusDto(bool IsConfigured, string ClientId, string TenantId, string RedirectUri, List<string> Scopes, string SetupHint);
public record GraphAuthStartResponse(string AuthorizationUrl, string State, string RedirectUri);
public record GraphAuthCallbackResponse(bool Success, string Message, string Scope, int ExpiresIn, string TokenType);
public record JiraIntegrationStatusDto(bool IsConfigured, string BaseUrl, string AuthMode, string AccountEmail, string SetupHint);
public record JiraTicketDto(string Key, string Summary, string Status, string StatusCategory, string Priority, string IssueType, string AssigneeName, string AssigneeEmail, string Url, DateTime? UpdatedAt, DateOnly? DueDate);
public record JiraAssigneeTicketsDto(string AssigneeName, string AssigneeEmail, int TotalTickets, int TodoTickets, int InProgressTickets, int DoneTickets, List<JiraTicketDto> Tickets);
public record JiraProjectTicketsDto(Guid ProjectId, string ProjectName, string BoardName, string ProjectKey, string Jql, int TotalTickets, int UnassignedTickets, List<JiraAssigneeTicketsDto> Assignees, List<JiraTicketDto> Tickets);

// AI extended DTOs
public record GraphWebhookNotification(string SubscriptionId, string ChangeType, string Resource, string TenantId, DateTime SubscriptionExpirationDateTime, string ClientState);
public record GraphWebhookValidationResponse(string ValidationToken);
public record WebhookProcessResult(bool Success, string? ProjectName, int TasksCreated, int DecisionsCreated, int RisksCreated, string? Error);
public record PortfolioSummaryDto(List<ProjectDto> Projects, int TotalProjects, int GreenCount, int YellowCount, int RedCount);

// ─── Command System DTOs ──────────────────────────────────────────────────────
public record CommandRequest(string Input, Guid? ProjectId = null);
public record CommandResult(
    string Command, string Args, string Title, string Summary,
    string Severity,
    List<CommandSection> Sections,
    List<CommandAction> SuggestedActions,
    string GeneratedAt);
public record CommandSection(string Title, string Icon, string Severity, List<CommandItem> Items);
public record CommandItem(
    string Title, string? Detail, string? ProjectName,
    string? AssigneeName, string? DueDate, string? Status,
    string? Priority, string? Score, string Severity,
    List<CommandAction> Actions);
public record CommandAction(
    string Label, string ActionType,
    string? EntityId, string? ProjectId, string? Payload);
