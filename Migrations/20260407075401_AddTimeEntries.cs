using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PmTool.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddTimeEntries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ActivityLogs_Users_UserId",
                table: "ActivityLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_ResourceAllocations_Users_UserId",
                table: "ResourceAllocations");

            migrationBuilder.DropForeignKey(
                name: "FK_TaskComments_Users_AuthorId",
                table: "TaskComments");

            migrationBuilder.DropForeignKey(
                name: "FK_Tasks_Users_AssigneeId",
                table: "Tasks");

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "Projects",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DeliveryModel",
                table: "Projects",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ExecutiveSummary",
                table: "Projects",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "HealthSummary",
                table: "Projects",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Sponsor",
                table: "Projects",
                type: "TEXT",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Stage",
                table: "Projects",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "AiSuggestionFeedback",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProjectId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SuggestionType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    SuggestionTitle = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiSuggestionFeedback", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AiSuggestionFeedback_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AiSuggestionFeedback_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AuditEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProjectId = table.Column<Guid>(type: "TEXT", nullable: true),
                    EntityId = table.Column<Guid>(type: "TEXT", nullable: true),
                    EntityType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ChangeType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    FromValue = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    ToValue = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Detail = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditEntries_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AuditEntries_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AuditEntries_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProjectDecisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProjectId = table.Column<Guid>(type: "TEXT", nullable: false),
                    OwnerId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Context = table.Column<string>(type: "TEXT", nullable: false),
                    Decision = table.Column<string>(type: "TEXT", nullable: false),
                    DueDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectDecisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectDecisions_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectDecisions_Users_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProjectDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProjectId = table.Column<Guid>(type: "TEXT", nullable: false),
                    OwnerId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Category = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Url = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectDocuments_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectDocuments_Users_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProjectForecastSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProjectId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SnapshotDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    BudgetAtCompletion = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ActualCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    EarnedValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PlannedValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    EstimateAtCompletion = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    EstimateToComplete = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CostPerformanceIndex = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    SchedulePerformanceIndex = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    TotalEstimatedHours = table.Column<int>(type: "INTEGER", nullable: false),
                    LoggedHours = table.Column<int>(type: "INTEGER", nullable: false),
                    RemainingHours = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectForecastSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectForecastSnapshots_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProjectGovernanceChecks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProjectId = table.Column<Guid>(type: "TEXT", nullable: false),
                    OwnerId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Area = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: false),
                    DueDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectGovernanceChecks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectGovernanceChecks_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectGovernanceChecks_Users_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProjectJiraLinks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProjectId = table.Column<Guid>(type: "TEXT", nullable: false),
                    BoardName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ProjectKey = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    BoardId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    JqlFilter = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    SyncStatus = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    LastSyncAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectJiraLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectJiraLinks_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProjectKnowledgeItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProjectId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AuthorId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    SourceType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    SourceLabel = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Category = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    SourceFileName = table.Column<string>(type: "TEXT", maxLength: 260, nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    ParentKnowledgeItemId = table.Column<Guid>(type: "TEXT", nullable: true),
                    LinkedEntityType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    LinkedEntityId = table.Column<Guid>(type: "TEXT", nullable: true),
                    MeetingReference = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Content = table.Column<string>(type: "TEXT", nullable: false),
                    TagsCsv = table.Column<string>(type: "TEXT", nullable: false),
                    Importance = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectKnowledgeItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectKnowledgeItems_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectKnowledgeItems_Users_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProjectMilestones",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProjectId = table.Column<Guid>(type: "TEXT", nullable: false),
                    OwnerId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    DueDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectMilestones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectMilestones_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectMilestones_Users_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProjectStageGates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProjectId = table.Column<Guid>(type: "TEXT", nullable: false),
                    OwnerId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    StageKey = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    GateOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    DueDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: false),
                    ApprovalSummary = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectStageGates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectStageGates_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectStageGates_Users_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProjectTeamsLinks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProjectId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TeamName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ChannelName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    TeamId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ChannelId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    TenantDomain = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    SyncStatus = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    LastSyncAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectTeamsLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectTeamsLinks_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TimeEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProjectId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Year = table.Column<int>(type: "INTEGER", nullable: false),
                    Month = table.Column<int>(type: "INTEGER", nullable: false),
                    Day = table.Column<int>(type: "INTEGER", nullable: false),
                    GeleistetHours = table.Column<decimal>(type: "TEXT", nullable: false),
                    FakturiertHours = table.Column<decimal>(type: "TEXT", nullable: false),
                    Comment = table.Column<string>(type: "TEXT", nullable: false),
                    ServiceType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TimeEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TimeEntries_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TimeEntries_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TimeEntries_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TimeEntryNotifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ForUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SubmittedByUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProjectId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Year = table.Column<int>(type: "INTEGER", nullable: false),
                    Month = table.Column<int>(type: "INTEGER", nullable: false),
                    Message = table.Column<string>(type: "TEXT", nullable: false),
                    IsRead = table.Column<bool>(type: "INTEGER", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TimeEntryNotifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TimeEntryNotifications_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TimeEntryNotifications_Users_SubmittedByUserId",
                        column: x => x.SubmittedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProjectApprovals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProjectId = table.Column<Guid>(type: "TEXT", nullable: false),
                    StageGateId = table.Column<Guid>(type: "TEXT", nullable: true),
                    RequestedById = table.Column<Guid>(type: "TEXT", nullable: false),
                    DecidedById = table.Column<Guid>(type: "TEXT", nullable: true),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ApprovalType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    DueDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    DecisionNotes = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectApprovals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectApprovals_ProjectStageGates_StageGateId",
                        column: x => x.StageGateId,
                        principalTable: "ProjectStageGates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectApprovals_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectApprovals_Users_DecidedById",
                        column: x => x.DecidedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectApprovals_Users_RequestedById",
                        column: x => x.RequestedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProjectStageGateChecks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    StageGateId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    RequirementType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    IsMandatory = table.Column<bool>(type: "INTEGER", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectStageGateChecks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectStageGateChecks_ProjectStageGates_StageGateId",
                        column: x => x.StageGateId,
                        principalTable: "ProjectStageGates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "AiSuggestionFeedback",
                columns: new[] { "Id", "CreatedAt", "Notes", "ProjectId", "Status", "SuggestionTitle", "SuggestionType", "UpdatedAt", "UserId" },
                values: new object[] { new Guid("c1c1c1c1-c1c1-c1c1-c1c1-c1c1c1c1c1c1"), new DateTime(2026, 3, 24, 8, 30, 0, 0, DateTimeKind.Utc), "Wird im kommenden Steering direkt aufgenommen.", new Guid("33333333-3333-3333-3333-333333333333"), "accepted", "Governance-Luecke schliessen", "governance", new DateTime(2026, 3, 24, 8, 30, 0, 0, DateTimeKind.Utc), new Guid("22222222-2222-2222-2222-222222222222") });

            migrationBuilder.InsertData(
                table: "ProjectDecisions",
                columns: new[] { "Id", "Context", "CreatedAt", "Decision", "DueDate", "OwnerId", "ProjectId", "Status", "Title", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("81818181-8181-8181-8181-818181818181"), "Es muss entschieden werden, ob Dokumente nach Projektphase oder Fachbereich organisiert werden.", new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4909), "Projektphase bleibt der primaere Einstieg, Fachbereich wird als Filter abgebildet.", new DateOnly(2026, 3, 29), new Guid("22222222-2222-2222-2222-222222222222"), new Guid("33333333-3333-3333-3333-333333333333"), "done", "Dokumentstruktur finalisieren", new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4909) },
                    { new Guid("82828282-8282-8282-8282-828282828282"), "Klärung, ob zuerst Teams oder direkt Copilot priorisiert wird.", new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4915), "Zuerst Teams Plugin, danach Copilot-Integration mit denselben Prompt-Bausteinen.", new DateOnly(2026, 4, 4), new Guid("77777777-7777-7777-7777-777777777777"), new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), "open", "Copilot Zielarchitektur", new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4915) }
                });

            migrationBuilder.InsertData(
                table: "ProjectDocuments",
                columns: new[] { "Id", "Category", "CreatedAt", "OwnerId", "ProjectId", "Status", "Title", "UpdatedAt", "Url" },
                values: new object[,]
                {
                    { new Guid("91919191-9191-9191-9191-919191919191"), "Projektakte", new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4939), new Guid("22222222-2222-2222-2222-222222222222"), new Guid("33333333-3333-3333-3333-333333333333"), "approved", "Projektsteckbrief", new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4939), "https://gshare.realcore.local/docs/g-share/steckbrief" },
                    { new Guid("92929292-9292-9292-9292-929292929292"), "Konzept", new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4942), new Guid("77777777-7777-7777-7777-777777777777"), new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), "draft", "Teams Plugin Konzept", new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4942), "https://gshare.realcore.local/docs/ai-briefing/teams-plugin" }
                });

            migrationBuilder.InsertData(
                table: "ProjectGovernanceChecks",
                columns: new[] { "Id", "Area", "CreatedAt", "DueDate", "Notes", "OwnerId", "ProjectId", "Status", "Title", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("a1a1a1a1-a1a1-a1a1-a1a1-a1a1a1a1a1a1"), "Kickoff", new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4964), new DateOnly(2026, 3, 27), "Projektziele, Scope und Verantwortungen sind hinterlegt.", new Guid("22222222-2222-2222-2222-222222222222"), new Guid("33333333-3333-3333-3333-333333333333"), "done", "Projektsteckbrief gepflegt", new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4964) },
                    { new Guid("a2a2a2a2-a2a2-a2a2-a2a2-a2a2a2a2a2a2"), "Steering", new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4967), new DateOnly(2026, 4, 3), "Sales und Management muessen den Pilotumfang bestaetigen.", new Guid("22222222-2222-2222-2222-222222222222"), new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), "open", "Stakeholder-Review geplant", new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4967) }
                });

            migrationBuilder.InsertData(
                table: "ProjectKnowledgeItems",
                columns: new[] { "Id", "AuthorId", "Category", "Content", "CreatedAt", "Importance", "LinkedEntityId", "LinkedEntityType", "MeetingReference", "ParentKnowledgeItemId", "ProjectId", "SourceFileName", "SourceLabel", "SourceType", "TagsCsv", "Title", "UpdatedAt", "Version" },
                values: new object[,]
                {
                    { new Guid("b1b1b1b1-b1b1-b1b1-b1b1-b1b1b1b1b1b1"), new Guid("22222222-2222-2222-2222-222222222222"), "general", "Management erwartet eine belastbare Dokumentenstruktur, Suchfunktion und klare Verantwortlichkeiten fuer Projektakten. Entscheidung: Dokumente werden nach Projektphase gefuehrt und ueber Tags zusaetzlich nach Fachbereich filterbar gemacht.", new DateTime(2026, 3, 21, 8, 0, 0, 0, DateTimeKind.Utc), 5, null, "", "", null, new Guid("33333333-3333-3333-3333-333333333333"), "", "Steering 21.03.2026", "meeting", "steering|dokumente|governance", "Steering Protokoll Maerz", new DateTime(2026, 3, 21, 8, 0, 0, 0, DateTimeKind.Utc), 1 },
                    { new Guid("b2b2b2b2-b2b2-b2b2-b2b2-b2b2b2b2b2b2"), new Guid("77777777-7777-7777-7777-777777777777"), "general", "Teams wird als erster Einstieg priorisiert. Nutzer wollen Management-Briefings direkt aus einem Projektkanal starten. Offener Punkt: Quellenbezug muss im Briefing sichtbar bleiben.", new DateTime(2026, 3, 20, 10, 30, 0, 0, DateTimeKind.Utc), 4, null, "", "", null, new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), "", "Workshop Teams Integration", "meeting", "teams|copilot|briefing", "Teams Discovery Notes", new DateTime(2026, 3, 20, 10, 30, 0, 0, DateTimeKind.Utc), 1 }
                });

            migrationBuilder.UpdateData(
                table: "ProjectLeadTasks",
                keyColumn: "Id",
                keyValue: new Guid("19191919-1919-1919-1919-191919191919"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4845), new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4845) });

            migrationBuilder.UpdateData(
                table: "ProjectLeadTasks",
                keyColumn: "Id",
                keyValue: new Guid("20202020-2020-2020-2020-202020202020"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4848), new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4849) });

            migrationBuilder.UpdateData(
                table: "ProjectLeadTasks",
                keyColumn: "Id",
                keyValue: new Guid("21212121-2121-2121-2121-212121212121"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4850), new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4851) });

            migrationBuilder.UpdateData(
                table: "ProjectLeadTasks",
                keyColumn: "Id",
                keyValue: new Guid("22222221-2222-2222-2222-222222222221"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4852), new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4853) });

            migrationBuilder.UpdateData(
                table: "ProjectLeadTasks",
                keyColumn: "Id",
                keyValue: new Guid("23232323-2323-2323-2323-232323232323"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4854), new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4855) });

            migrationBuilder.InsertData(
                table: "ProjectMilestones",
                columns: new[] { "Id", "CreatedAt", "Description", "DueDate", "OwnerId", "ProjectId", "Status", "Title", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("71717171-7171-7171-7171-717171717171"), new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4881), "Projektakten, Ordnerstruktur und Dokumentenlisten in Produktion schalten.", new DateOnly(2026, 4, 5), new Guid("22222222-2222-2222-2222-222222222222"), new Guid("33333333-3333-3333-3333-333333333333"), "in_progress", "Dokumentenablage Release", new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4881) },
                    { new Guid("72727272-7272-7272-7272-727272727272"), new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4884), "Einbindung in Microsoft Teams fachlich und technisch finalisieren.", new DateOnly(2026, 4, 10), new Guid("77777777-7777-7777-7777-777777777777"), new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), "planned", "Teams Plugin Konzept", new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4884) }
                });

            migrationBuilder.InsertData(
                table: "ProjectTeamsLinks",
                columns: new[] { "Id", "ChannelId", "ChannelName", "CreatedAt", "LastSyncAt", "ProjectId", "SyncStatus", "TeamId", "TeamName", "TenantDomain", "UpdatedAt" },
                values: new object[] { new Guid("d1d1d1d1-d1d1-d1d1-d1d1-d1d1d1d1d1d1"), "channel-briefing-pilot", "briefing-pilot", new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(5131), null, new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), "planned", "teams-ai-briefing", "AI Briefing Tool", "realcore.onmicrosoft.com", new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(5131) });

            migrationBuilder.UpdateData(
                table: "Projects",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                columns: new[] { "Category", "CreatedAt", "DeliveryModel", "Description", "ExecutiveSummary", "HealthSummary", "NextMilestone", "Objective", "Scope", "Sponsor", "Stage", "TechnologiesCsv", "UpdatedAt" },
                values: new object[] { "product", new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4568), "Produktentwicklung", "Dokumentenablage und zentrale Plattform fuer Projektsteuerung, Teamtransparenz und operative Zusammenarbeit.", "G-Share ist das zentrale Arbeitsmittel fuer Delivery, Projektleitung und Dokumentenablage.", "Scope ist stabil, Fokus liegt auf echter Projektsteuerung, Suchbarkeit und Governance-Faehigkeit.", "Dokumentenablage und Governance-Cockpit live schalten", "Ein zentrales Tool schaffen, in dem Projekte, Teams, Aufgaben, Entscheidungen und Dokumente an einem Ort gepflegt werden.", "Portfolio, Projekt-Detail, Ressourcen, Aufgabensteuerung, Notizen, Dokumente, Governance und Projektleiter-Workflows.", "Management", "delivery", "Next.js|TypeScript|Zustand|Tailwind CSS|ASP.NET Core", new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4569) });

            migrationBuilder.UpdateData(
                table: "Projects",
                keyColumn: "Id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                columns: new[] { "Category", "CreatedAt", "DeliveryModel", "Description", "ExecutiveSummary", "HealthSummary", "NextMilestone", "Sponsor", "Stage", "TechnologiesCsv", "UpdatedAt" },
                values: new object[] { "product", new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4595), "Produktentwicklung", "Plugin fuer Teams und spaeter Copilot fuer schnelle Management-Briefings mit Projektstatus, Risiken und To-dos aus einer Quelle.", "Das Tool soll aus Live-Projektdaten belastbare Briefings fuer Management und Delivery erzeugen.", "Funktionsfaehiger Kern vorhanden, der naechste Hebel ist die Einbettung in Teams und Copilot-Szenarien.", "Teams-Plugin-Konzept finalisieren", "Sales & Management", "pilot", "Next.js|TypeScript|LLM Integration|Microsoft Teams", new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4596) });

            migrationBuilder.InsertData(
                table: "Projects",
                columns: new[] { "Id", "BudgetSpent", "BudgetTotal", "Category", "Communication", "CreatedAt", "Customer", "DeliveryModel", "Description", "EndDate", "ExecutiveSummary", "HealthSummary", "Name", "NextMilestone", "Objective", "OwnerId", "ProgressPercent", "Scope", "Sponsor", "Stage", "StakeholdersCsv", "StartDate", "Status", "SuccessMetric", "TechnologiesCsv", "TenantId", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("34343434-3434-3434-3434-343434343434"), 34000m, 85000m, "delivery", "Jour fixe mit Fachbereich donnerstags, Umsetzungsreview montags.", new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4601), "Interner Fachbereich", "Power Platform Delivery", "Erweiterung der bestehenden PowerApp mit Freeform-Komponenten. Philipp ist in der Umsetzung.", new DateOnly(2026, 8, 15), "Ein bestaetigtes Delivery-Projekt mit klarer Umsetzungslinie ueber Philipp und enger Fachbereichsabstimmung.", "Liegt auf Kurs, benoetigt aber saubere Entscheidungs- und Change-Request-Steuerung.", "PowerApp Erweiterung - Freeform", "Fachbereichs-Review fuer die erste Freeform-Strecke", "Die bestehende PowerApp fachlich und technisch erweitern, ohne die bestehende Nutzung zu stoeren.", new Guid("22222222-2222-2222-2222-222222222222"), 41, "Freeform-Komponenten, Fachbereichsfeedback, Change Requests, Test und Uebergabe.", "Fachbereich Operations", "implementation", "Philipp|Fachbereich|Projektleitung", new DateOnly(2026, 3, 1), "green", "Der Fachbereich kann neue Freeform-Strecken ohne Medienbruch in der App nutzen.", "Power Apps|Power Automate|Dataverse", new Guid("11111111-1111-1111-1111-111111111111"), new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4601) },
                    { new Guid("45454545-4545-4545-4545-454545454545"), 28000m, 220000m, "rollout", "Rollout-Abstimmung dienstags, Technikreview freitags.", new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4608), "Retail Operations", "Custom Development", "React-basierte POS-App. Standorte benoetigen Kassenpakete; Standortdaten kommen initial aus Excel.", new DateOnly(2026, 10, 30), "Das Projekt verbindet Entwicklung, Rollout-Planung, Standortdaten und operative Kassenprozesse.", "Anforderungs- und Datenlage sind noch volatil; Governance und Meilensteinsteuerung sind hier besonders wichtig.", "Bestellsystem POS App", "Excel-Import und Standortmodell abnehmen", "Ein skalierbares Bestellsystem fuer Standorte bereitstellen, das Kassenpakete und Standortdaten sauber verarbeitet.", new Guid("22222222-2222-2222-2222-222222222222"), 18, "Excel-Import, Standortdaten, Paketlogik, POS-UI, Rolloutplanung und Test je Standort.", "Retail Operations", "planning", "Retail Operations|Projektleitung|Entwicklung|Standorte", new DateOnly(2026, 3, 15), "yellow", "Ein Standort kann in unter 10 Minuten angelegt und einem Kassenpaket zugeordnet werden.", "React|TypeScript|Excel Import|REST API", new Guid("11111111-1111-1111-1111-111111111111"), new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4608) },
                    { new Guid("56565656-5656-5656-5656-565656565656"), 12000m, 60000m, "governance", "Governance Review jeden Mittwoch mit Projektleitung und Management.", new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4612), "Management", "PMO & Standards", "Querschnittsprojekt fuer PM-Standards, Stage Gates, Architektur, Risiko- und Entscheidungssteuerung.", new DateOnly(2026, 9, 30), "Governance ist der Hebel, um alle Projekte mit denselben Regeln, Gates und Berichtslinien zu steuern.", "Muss parallel zu den Projekten aufgebaut werden, damit Skalierung und Steuerbarkeit funktionieren.", "Governance", "Stage-Gate-Checkliste fuer alle Projekte einführen", "Ein PMO-taugliches Governance-Modul mit Standards, Freigaben und klaren Verantwortlichkeiten aufbauen.", new Guid("22222222-2222-2222-2222-222222222222"), 27, "Templates, Gate-Logik, Rollen, Standards, Review-Zyklen, Eskalationspfade.", "Management", "setup", "Management|Projektleitung|Architektur|Delivery Leads", new DateOnly(2026, 3, 20), "green", "Alle Projekte folgen denselben Pflichtfeldern, Entscheidungswegen und Abnahme-Gates.", "PM Framework|Governance|Reporting", new Guid("11111111-1111-1111-1111-111111111111"), new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4613) }
                });

            migrationBuilder.UpdateData(
                table: "ResourceAllocations",
                keyColumn: "Id",
                keyValue: new Guid("12121212-1212-1212-1212-121212121212"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4661), new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4661) });

            migrationBuilder.UpdateData(
                table: "ResourceAllocations",
                keyColumn: "Id",
                keyValue: new Guid("13131313-1313-1313-1313-131313131313"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4665), new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4665) });

            migrationBuilder.UpdateData(
                table: "ResourceAllocations",
                keyColumn: "Id",
                keyValue: new Guid("14141414-1414-1414-1414-141414141414"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4667), new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4667) });

            migrationBuilder.UpdateData(
                table: "ResourceAllocations",
                keyColumn: "Id",
                keyValue: new Guid("15151515-1515-1515-1515-151515151515"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4669), new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4669) });

            migrationBuilder.UpdateData(
                table: "ResourceAllocations",
                keyColumn: "Id",
                keyValue: new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4648), new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4648) });

            migrationBuilder.UpdateData(
                table: "ResourceAllocations",
                keyColumn: "Id",
                keyValue: new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4653), new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4654) });

            migrationBuilder.UpdateData(
                table: "ResourceAllocations",
                keyColumn: "Id",
                keyValue: new Guid("dddddddd-dddd-dddd-dddd-dddddddddddd"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4655), new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4656) });

            migrationBuilder.UpdateData(
                table: "ResourceAllocations",
                keyColumn: "Id",
                keyValue: new Guid("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4657), new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4657) });

            migrationBuilder.UpdateData(
                table: "ResourceAllocations",
                keyColumn: "Id",
                keyValue: new Guid("ffffffff-ffff-ffff-ffff-ffffffffffff"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4659), new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4659) });

            migrationBuilder.UpdateData(
                table: "Tenants",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4307), new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4310) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4428), new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4428) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4431), new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4431) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4433), new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4433) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("66666666-6666-6666-6666-666666666666"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4442), new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4442) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("77777777-7777-7777-7777-777777777777"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4444), new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4444) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("88888888-8888-8888-8888-888888888888"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4445), new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4446) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("99999999-9999-9999-9999-999999999999"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4447), new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4447) });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "CreatedAt", "DisplayName", "Email", "PasswordHash", "Role", "TenantId", "UpdatedAt" },
                values: new object[] { new Guid("10101010-1010-1010-1010-101010101010"), new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4448), "Philipp Schneider", "philipp@realcore.de", "$2a$11$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2uheWG/igi.", "Power Platform Developer", new Guid("11111111-1111-1111-1111-111111111111"), new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4448) });

            migrationBuilder.InsertData(
                table: "ProjectDecisions",
                columns: new[] { "Id", "Context", "CreatedAt", "Decision", "DueDate", "OwnerId", "ProjectId", "Status", "Title", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("83838383-8383-8383-8383-838383838383"), "Neue Felder koennen in Dataverse oder lokal in der App gehalten werden.", new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4917), "Noch offen, Dataverse-Pfad wird bewertet.", new DateOnly(2026, 3, 30), new Guid("10101010-1010-1010-1010-101010101010"), new Guid("34343434-3434-3434-3434-343434343434"), "open", "Freeform Datenmodell", new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4917) },
                    { new Guid("84848484-8484-8484-8484-848484848484"), "Excel ist Startpunkt, langfristig wird eine Schnittstelle erwartet.", new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4919), "Go-live mit Excel-Import, API-Schnittstelle als Folgephase.", new DateOnly(2026, 4, 8), new Guid("22222222-2222-2222-2222-222222222222"), new Guid("45454545-4545-4545-4545-454545454545"), "review", "Standortdatenquelle", new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4919) },
                    { new Guid("85858585-8585-8585-8585-858585858585"), "Standardisierte Gates muessen fuer alle Projekte verbindlich werden.", new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4921), "Kickoff, Scope Freeze, Test Readiness und Go-live sind Pflicht-Gates.", new DateOnly(2026, 3, 31), new Guid("22222222-2222-2222-2222-222222222222"), new Guid("56565656-5656-5656-5656-565656565656"), "done", "Pflicht-Gates fuer Delivery", new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4922) }
                });

            migrationBuilder.InsertData(
                table: "ProjectDocuments",
                columns: new[] { "Id", "Category", "CreatedAt", "OwnerId", "ProjectId", "Status", "Title", "UpdatedAt", "Url" },
                values: new object[,]
                {
                    { new Guid("93939393-9393-9393-9393-939393939393"), "Delivery", new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4944), new Guid("10101010-1010-1010-1010-101010101010"), new Guid("34343434-3434-3434-3434-343434343434"), "draft", "Freeform Umsetzungsnotizen", new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4944), "https://gshare.realcore.local/docs/freeform/umsetzung" },
                    { new Guid("94949494-9494-9494-9494-949494949494"), "Spezifikation", new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4945), new Guid("22222222-2222-2222-2222-222222222222"), new Guid("45454545-4545-4545-4545-454545454545"), "review", "Excel Datenmapping", new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4946), "https://gshare.realcore.local/docs/pos/excel-mapping" },
                    { new Guid("95959595-9595-9595-9595-959595959595"), "Governance", new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4949), new Guid("22222222-2222-2222-2222-222222222222"), new Guid("56565656-5656-5656-5656-565656565656"), "approved", "Governance Handbuch", new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4949), "https://gshare.realcore.local/docs/governance/handbuch" }
                });

            migrationBuilder.InsertData(
                table: "ProjectGovernanceChecks",
                columns: new[] { "Id", "Area", "CreatedAt", "DueDate", "Notes", "OwnerId", "ProjectId", "Status", "Title", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("a3a3a3a3-a3a3-a3a3-a3a3-a3a3a3a3a3a3"), "Scope Control", new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4969), new DateOnly(2026, 4, 2), "Fachbereichsaenderungen duerfen nur ueber Change Request aufgenommen werden.", new Guid("22222222-2222-2222-2222-222222222222"), new Guid("34343434-3434-3434-3434-343434343434"), "open", "Change Request Prozess geklaert", new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4969) },
                    { new Guid("a4a4a4a4-a4a4-a4a4-a4a4-a4a4a4a4a4a4"), "Go-live", new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4971), new DateOnly(2026, 4, 15), "Standortdaten, Paketzuordnung und Teststand muessen vor Rollout vollstaendig sein.", new Guid("22222222-2222-2222-2222-222222222222"), new Guid("45454545-4545-4545-4545-454545454545"), "open", "Rollout-Gate vorbereitet", new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4971) },
                    { new Guid("a5a5a5a5-a5a5-a5a5-a5a5-a5a5a5a5a5a5"), "PMO", new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4973), new DateOnly(2026, 3, 31), "Pflicht-Gates fuer alle Projekte werden verbindlich dokumentiert.", new Guid("22222222-2222-2222-2222-222222222222"), new Guid("56565656-5656-5656-5656-565656565656"), "in_progress", "Standard-Gates dokumentiert", new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4973) }
                });

            migrationBuilder.InsertData(
                table: "ProjectKnowledgeItems",
                columns: new[] { "Id", "AuthorId", "Category", "Content", "CreatedAt", "Importance", "LinkedEntityId", "LinkedEntityType", "MeetingReference", "ParentKnowledgeItemId", "ProjectId", "SourceFileName", "SourceLabel", "SourceType", "TagsCsv", "Title", "UpdatedAt", "Version" },
                values: new object[,]
                {
                    { new Guid("b3b3b3b3-b3b3-b3b3-b3b3-b3b3b3b3b3b3"), new Guid("10101010-1010-1010-1010-101010101010"), "general", "Die Freeform-Strecke ist technisch umsetzbar. Kritisch ist noch die Validierung der Eingaben und die Frage, wie Fachbereichsaenderungen ohne Regression aufgenommen werden.", new DateTime(2026, 3, 22, 9, 20, 0, 0, DateTimeKind.Utc), 4, null, "", "", null, new Guid("34343434-3434-3434-3434-343434343434"), "", "Philipp Update", "delivery_note", "powerapp|validierung|change-request", "Freeform Validierung", new DateTime(2026, 3, 22, 9, 20, 0, 0, DateTimeKind.Utc), 1 },
                    { new Guid("b4b4b4b4-b4b4-b4b4-b4b4-b4b4b4b4b4b4"), new Guid("22222222-2222-2222-2222-222222222222"), "general", "Die Standortliste ist unvollstaendig. Drei Standorte haben kein Kassenpaket, mehrere Zeilen enthalten doppelte IDs. Vor dem Rollout muss das Mapping mit Operations bereinigt werden.", new DateTime(2026, 3, 24, 7, 45, 0, 0, DateTimeKind.Utc), 5, null, "", "", null, new Guid("45454545-4545-4545-4545-454545454545"), "", "Standorte_Maerz.xlsx", "import", "excel|standorte|rollout", "Excel Import Erkenntnisse", new DateTime(2026, 3, 24, 7, 45, 0, 0, DateTimeKind.Utc), 1 },
                    { new Guid("b5b5b5b5-b5b5-b5b5-b5b5-b5b5b5b5b5b5"), new Guid("22222222-2222-2222-2222-222222222222"), "general", "Jedes Delivery-Projekt benoetigt ab sofort Pflicht-Gates, einen Entscheidungslog und einen nachweisbaren Abnahmestand. Das System soll Luecken automatisch markieren und Vorschlaege generieren.", new DateTime(2026, 3, 24, 8, 15, 0, 0, DateTimeKind.Utc), 5, null, "", "", null, new Guid("56565656-5656-5656-5656-565656565656"), "", "PMO Workshop", "meeting", "pmo|stage-gate|standards", "Governance Zielbild Workshop", new DateTime(2026, 3, 24, 8, 15, 0, 0, DateTimeKind.Utc), 1 }
                });

            migrationBuilder.InsertData(
                table: "ProjectLeadTasks",
                columns: new[] { "Id", "CreatedAt", "Description", "DueDate", "OwnerId", "ProjectId", "Status", "Title", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("24242424-2424-2424-2424-242424242424"), new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4856), "Fachliche und technische Review-Punkte fuer die Freeform-Erweiterung vorbereiten.", new DateOnly(2026, 3, 28), new Guid("22222222-2222-2222-2222-222222222222"), new Guid("34343434-3434-3434-3434-343434343434"), "todo", "Review mit Philipp vorbereiten", new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4857) },
                    { new Guid("25252525-2525-2525-2525-252525252525"), new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4860), "Kassenpakete und Rollout-Zuordnung fachlich finalisieren.", new DateOnly(2026, 4, 2), new Guid("22222222-2222-2222-2222-222222222222"), new Guid("45454545-4545-4545-4545-454545454545"), "in_progress", "Standortpakete definieren", new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4860) },
                    { new Guid("26262626-2626-2626-2626-262626262626"), new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4862), "Pflichtfelder fuer Kickoff, Delivery und Go-Live definieren.", new DateOnly(2026, 3, 31), new Guid("22222222-2222-2222-2222-222222222222"), new Guid("56565656-5656-5656-5656-565656565656"), "todo", "Stage Gate Pflichtfelder festlegen", new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4862) }
                });

            migrationBuilder.InsertData(
                table: "ProjectMilestones",
                columns: new[] { "Id", "CreatedAt", "Description", "DueDate", "OwnerId", "ProjectId", "Status", "Title", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("73737373-7373-7373-7373-737373737373"), new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4886), "Erste fachliche Abnahme der Freeform-Strecke.", new DateOnly(2026, 4, 3), new Guid("10101010-1010-1010-1010-101010101010"), new Guid("34343434-3434-3434-3434-343434343434"), "planned", "Freeform Review", new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4887) },
                    { new Guid("74747474-7474-7474-7474-747474747474"), new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4888), "Standortmodell und Importlogik gemeinsam mit Operations pruefen.", new DateOnly(2026, 4, 12), new Guid("22222222-2222-2222-2222-222222222222"), new Guid("45454545-4545-4545-4545-454545454545"), "planned", "Excel Import Abnahme", new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4888) },
                    { new Guid("75757575-7575-7575-7575-757575757575"), new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4890), "Pflichtcheckliste fuer neue Projekte ausrollen.", new DateOnly(2026, 4, 1), new Guid("22222222-2222-2222-2222-222222222222"), new Guid("56565656-5656-5656-5656-565656565656"), "in_progress", "Governance Gate v1", new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4890) }
                });

            migrationBuilder.InsertData(
                table: "ProjectNotes",
                columns: new[] { "Id", "AuthorId", "Content", "CreatedAt", "ProjectId", "Title", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("19181818-1818-1818-1818-181818181819"), new Guid("10101010-1010-1010-1010-101010101010"), "Philipp arbeitet an der ersten Freeform-Strecke, offene Punkte betreffen Datenvalidierung.", new DateTime(2026, 3, 22, 9, 15, 0, 0, DateTimeKind.Utc), new Guid("34343434-3434-3434-3434-343434343434"), "Umsetzungsstand", new DateTime(2026, 3, 22, 9, 15, 0, 0, DateTimeKind.Utc) },
                    { new Guid("20282828-2828-2828-2828-282828282828"), new Guid("22222222-2222-2222-2222-222222222222"), "Excel-Import ist initial gesetzt, spaeter ist eine Schnittstelle zur Stammdatenquelle geplant.", new DateTime(2026, 3, 24, 7, 30, 0, 0, DateTimeKind.Utc), new Guid("45454545-4545-4545-4545-454545454545"), "Projektansatz", new DateTime(2026, 3, 24, 7, 30, 0, 0, DateTimeKind.Utc) },
                    { new Guid("30383838-3838-3838-3838-383838383838"), new Guid("22222222-2222-2222-2222-222222222222"), "Alle Delivery-Projekte sollen ueber dieselben Pflichtfelder, Gates und Freigabeschritte gefuehrt werden.", new DateTime(2026, 3, 24, 8, 0, 0, 0, DateTimeKind.Utc), new Guid("56565656-5656-5656-5656-565656565656"), "Governance Zielbild", new DateTime(2026, 3, 24, 8, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "ProjectTeamsLinks",
                columns: new[] { "Id", "ChannelId", "ChannelName", "CreatedAt", "LastSyncAt", "ProjectId", "SyncStatus", "TeamId", "TeamName", "TenantDomain", "UpdatedAt" },
                values: new object[] { new Guid("d2d2d2d2-d2d2-d2d2-d2d2-d2d2d2d2d2d2"), "channel-weekly-sync", "weekly-sync", new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(5135), null, new Guid("45454545-4545-4545-4545-454545454545"), "planned", "teams-pos-rollout", "POS Rollout", "realcore.onmicrosoft.com", new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(5135) });

            migrationBuilder.InsertData(
                table: "ResourceAllocations",
                columns: new[] { "Id", "AllocatedHours", "CreatedAt", "ProjectId", "ProjectRole", "Responsibility", "UpdatedAt", "UserId" },
                values: new object[,]
                {
                    { new Guid("61616161-6161-6161-6161-616161616161"), 12, new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4670), new Guid("34343434-3434-3434-3434-343434343434"), "Projektleiter", "Steuerung, Fachbereich, Abnahme", new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4671), new Guid("22222222-2222-2222-2222-222222222222") },
                    { new Guid("62626262-6262-6262-6262-626262626262"), 36, new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4672), new Guid("34343434-3434-3434-3434-343434343434"), "Lead Developer", "Umsetzung der Freeform-Erweiterung", new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4673), new Guid("10101010-1010-1010-1010-101010101010") },
                    { new Guid("63636363-6363-6363-6363-636363636363"), 10, new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4674), new Guid("34343434-3434-3434-3434-343434343434"), "Fachliche Steuerung", "Anforderungen und Priorisierung", new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4674), new Guid("44444444-4444-4444-4444-444444444444") },
                    { new Guid("64646464-6464-6464-6464-646464646464"), 20, new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4676), new Guid("45454545-4545-4545-4545-454545454545"), "Projektleiter", "Rollout-Steuerung, Stakeholder, Risiko-Management", new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4676), new Guid("22222222-2222-2222-2222-222222222222") },
                    { new Guid("65656565-6565-6565-6565-656565656565"), 28, new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4678), new Guid("45454545-4545-4545-4545-454545454545"), "React Developer", "POS Frontend und Standort-UI", new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4678), new Guid("55555555-5555-5555-5555-555555555555") },
                    { new Guid("66666661-6666-6666-6666-666666666661"), 24, new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4682), new Guid("45454545-4545-4545-4545-454545454545"), "Backend Developer", "Import, Standortdaten, Schnittstellen", new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4682), new Guid("66666666-6666-6666-6666-666666666666") },
                    { new Guid("67676767-6767-6767-6767-676767676767"), 14, new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4683), new Guid("56565656-5656-5656-5656-565656565656"), "Programmleitung", "Standards, Stage Gates, Reporting", new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4684), new Guid("22222222-2222-2222-2222-222222222222") },
                    { new Guid("68686868-6868-6868-6868-686868686868"), 8, new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4685), new Guid("56565656-5656-5656-5656-565656565656"), "PMO", "Templates, Pflichtfelder, Review-Prozesse", new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4685), new Guid("44444444-4444-4444-4444-444444444444") },
                    { new Guid("69696969-6969-6969-6969-696969696969"), 6, new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4687), new Guid("56565656-5656-5656-5656-565656565656"), "Quality", "Abnahme- und Gate-Checklisten", new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4687), new Guid("99999999-9999-9999-9999-999999999999") }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AiSuggestionFeedback_ProjectId",
                table: "AiSuggestionFeedback",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_AiSuggestionFeedback_UserId",
                table: "AiSuggestionFeedback",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditEntries_ProjectId",
                table: "AuditEntries",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditEntries_TenantId",
                table: "AuditEntries",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditEntries_UserId",
                table: "AuditEntries",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectApprovals_DecidedById",
                table: "ProjectApprovals",
                column: "DecidedById");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectApprovals_ProjectId",
                table: "ProjectApprovals",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectApprovals_RequestedById",
                table: "ProjectApprovals",
                column: "RequestedById");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectApprovals_StageGateId",
                table: "ProjectApprovals",
                column: "StageGateId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectDecisions_OwnerId",
                table: "ProjectDecisions",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectDecisions_ProjectId",
                table: "ProjectDecisions",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectDocuments_OwnerId",
                table: "ProjectDocuments",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectDocuments_ProjectId",
                table: "ProjectDocuments",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectForecastSnapshots_ProjectId_SnapshotDate",
                table: "ProjectForecastSnapshots",
                columns: new[] { "ProjectId", "SnapshotDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectGovernanceChecks_OwnerId",
                table: "ProjectGovernanceChecks",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectGovernanceChecks_ProjectId",
                table: "ProjectGovernanceChecks",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectJiraLinks_ProjectId",
                table: "ProjectJiraLinks",
                column: "ProjectId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectKnowledgeItems_AuthorId",
                table: "ProjectKnowledgeItems",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectKnowledgeItems_ProjectId",
                table: "ProjectKnowledgeItems",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectMilestones_OwnerId",
                table: "ProjectMilestones",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectMilestones_ProjectId",
                table: "ProjectMilestones",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectStageGateChecks_StageGateId",
                table: "ProjectStageGateChecks",
                column: "StageGateId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectStageGates_OwnerId",
                table: "ProjectStageGates",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectStageGates_ProjectId",
                table: "ProjectStageGates",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTeamsLinks_ProjectId",
                table: "ProjectTeamsLinks",
                column: "ProjectId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TimeEntries_ProjectId",
                table: "TimeEntries",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_TimeEntries_TenantId_ProjectId_UserId_Year_Month_Day",
                table: "TimeEntries",
                columns: new[] { "TenantId", "ProjectId", "UserId", "Year", "Month", "Day" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TimeEntries_UserId",
                table: "TimeEntries",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_TimeEntryNotifications_ProjectId",
                table: "TimeEntryNotifications",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_TimeEntryNotifications_SubmittedByUserId",
                table: "TimeEntryNotifications",
                column: "SubmittedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TimeEntryNotifications_TenantId_ForUserId_IsRead",
                table: "TimeEntryNotifications",
                columns: new[] { "TenantId", "ForUserId", "IsRead" });

            migrationBuilder.AddForeignKey(
                name: "FK_ActivityLogs_Users_UserId",
                table: "ActivityLogs",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ResourceAllocations_Users_UserId",
                table: "ResourceAllocations",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TaskComments_Users_AuthorId",
                table: "TaskComments",
                column: "AuthorId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Tasks_Users_AssigneeId",
                table: "Tasks",
                column: "AssigneeId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ActivityLogs_Users_UserId",
                table: "ActivityLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_ResourceAllocations_Users_UserId",
                table: "ResourceAllocations");

            migrationBuilder.DropForeignKey(
                name: "FK_TaskComments_Users_AuthorId",
                table: "TaskComments");

            migrationBuilder.DropForeignKey(
                name: "FK_Tasks_Users_AssigneeId",
                table: "Tasks");

            migrationBuilder.DropTable(
                name: "AiSuggestionFeedback");

            migrationBuilder.DropTable(
                name: "AuditEntries");

            migrationBuilder.DropTable(
                name: "ProjectApprovals");

            migrationBuilder.DropTable(
                name: "ProjectDecisions");

            migrationBuilder.DropTable(
                name: "ProjectDocuments");

            migrationBuilder.DropTable(
                name: "ProjectForecastSnapshots");

            migrationBuilder.DropTable(
                name: "ProjectGovernanceChecks");

            migrationBuilder.DropTable(
                name: "ProjectJiraLinks");

            migrationBuilder.DropTable(
                name: "ProjectKnowledgeItems");

            migrationBuilder.DropTable(
                name: "ProjectMilestones");

            migrationBuilder.DropTable(
                name: "ProjectStageGateChecks");

            migrationBuilder.DropTable(
                name: "ProjectTeamsLinks");

            migrationBuilder.DropTable(
                name: "TimeEntries");

            migrationBuilder.DropTable(
                name: "TimeEntryNotifications");

            migrationBuilder.DropTable(
                name: "ProjectStageGates");

            migrationBuilder.DeleteData(
                table: "ProjectLeadTasks",
                keyColumn: "Id",
                keyValue: new Guid("24242424-2424-2424-2424-242424242424"));

            migrationBuilder.DeleteData(
                table: "ProjectLeadTasks",
                keyColumn: "Id",
                keyValue: new Guid("25252525-2525-2525-2525-252525252525"));

            migrationBuilder.DeleteData(
                table: "ProjectLeadTasks",
                keyColumn: "Id",
                keyValue: new Guid("26262626-2626-2626-2626-262626262626"));

            migrationBuilder.DeleteData(
                table: "ProjectNotes",
                keyColumn: "Id",
                keyValue: new Guid("19181818-1818-1818-1818-181818181819"));

            migrationBuilder.DeleteData(
                table: "ProjectNotes",
                keyColumn: "Id",
                keyValue: new Guid("20282828-2828-2828-2828-282828282828"));

            migrationBuilder.DeleteData(
                table: "ProjectNotes",
                keyColumn: "Id",
                keyValue: new Guid("30383838-3838-3838-3838-383838383838"));

            migrationBuilder.DeleteData(
                table: "ResourceAllocations",
                keyColumn: "Id",
                keyValue: new Guid("61616161-6161-6161-6161-616161616161"));

            migrationBuilder.DeleteData(
                table: "ResourceAllocations",
                keyColumn: "Id",
                keyValue: new Guid("62626262-6262-6262-6262-626262626262"));

            migrationBuilder.DeleteData(
                table: "ResourceAllocations",
                keyColumn: "Id",
                keyValue: new Guid("63636363-6363-6363-6363-636363636363"));

            migrationBuilder.DeleteData(
                table: "ResourceAllocations",
                keyColumn: "Id",
                keyValue: new Guid("64646464-6464-6464-6464-646464646464"));

            migrationBuilder.DeleteData(
                table: "ResourceAllocations",
                keyColumn: "Id",
                keyValue: new Guid("65656565-6565-6565-6565-656565656565"));

            migrationBuilder.DeleteData(
                table: "ResourceAllocations",
                keyColumn: "Id",
                keyValue: new Guid("66666661-6666-6666-6666-666666666661"));

            migrationBuilder.DeleteData(
                table: "ResourceAllocations",
                keyColumn: "Id",
                keyValue: new Guid("67676767-6767-6767-6767-676767676767"));

            migrationBuilder.DeleteData(
                table: "ResourceAllocations",
                keyColumn: "Id",
                keyValue: new Guid("68686868-6868-6868-6868-686868686868"));

            migrationBuilder.DeleteData(
                table: "ResourceAllocations",
                keyColumn: "Id",
                keyValue: new Guid("69696969-6969-6969-6969-696969696969"));

            migrationBuilder.DeleteData(
                table: "Projects",
                keyColumn: "Id",
                keyValue: new Guid("34343434-3434-3434-3434-343434343434"));

            migrationBuilder.DeleteData(
                table: "Projects",
                keyColumn: "Id",
                keyValue: new Guid("45454545-4545-4545-4545-454545454545"));

            migrationBuilder.DeleteData(
                table: "Projects",
                keyColumn: "Id",
                keyValue: new Guid("56565656-5656-5656-5656-565656565656"));

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("10101010-1010-1010-1010-101010101010"));

            migrationBuilder.DropColumn(
                name: "Category",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "DeliveryModel",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "ExecutiveSummary",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "HealthSummary",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "Sponsor",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "Stage",
                table: "Projects");

            migrationBuilder.UpdateData(
                table: "ProjectLeadTasks",
                keyColumn: "Id",
                keyValue: new Guid("19191919-1919-1919-1919-191919191919"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 23, 9, 59, 54, 364, DateTimeKind.Utc).AddTicks(6860), new DateTime(2026, 3, 23, 9, 59, 54, 364, DateTimeKind.Utc).AddTicks(6861) });

            migrationBuilder.UpdateData(
                table: "ProjectLeadTasks",
                keyColumn: "Id",
                keyValue: new Guid("20202020-2020-2020-2020-202020202020"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 23, 9, 59, 54, 364, DateTimeKind.Utc).AddTicks(6864), new DateTime(2026, 3, 23, 9, 59, 54, 364, DateTimeKind.Utc).AddTicks(6865) });

            migrationBuilder.UpdateData(
                table: "ProjectLeadTasks",
                keyColumn: "Id",
                keyValue: new Guid("21212121-2121-2121-2121-212121212121"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 23, 9, 59, 54, 364, DateTimeKind.Utc).AddTicks(6867), new DateTime(2026, 3, 23, 9, 59, 54, 364, DateTimeKind.Utc).AddTicks(6868) });

            migrationBuilder.UpdateData(
                table: "ProjectLeadTasks",
                keyColumn: "Id",
                keyValue: new Guid("22222221-2222-2222-2222-222222222221"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 23, 9, 59, 54, 364, DateTimeKind.Utc).AddTicks(6871), new DateTime(2026, 3, 23, 9, 59, 54, 364, DateTimeKind.Utc).AddTicks(6871) });

            migrationBuilder.UpdateData(
                table: "ProjectLeadTasks",
                keyColumn: "Id",
                keyValue: new Guid("23232323-2323-2323-2323-232323232323"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 23, 9, 59, 54, 364, DateTimeKind.Utc).AddTicks(6877), new DateTime(2026, 3, 23, 9, 59, 54, 364, DateTimeKind.Utc).AddTicks(6877) });

            migrationBuilder.UpdateData(
                table: "Projects",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                columns: new[] { "CreatedAt", "Description", "NextMilestone", "Objective", "Scope", "TechnologiesCsv", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 23, 9, 59, 54, 364, DateTimeKind.Utc).AddTicks(6718), "Zentrale Plattform fuer Projektsteuerung, Teamtransparenz und operative Zusammenarbeit.", "Projekt-Detailseite mit Team und Notizen live schalten", "Ein zentrales Tool schaffen, in dem Projekte, Teams, Aufgaben und Notizen an einem Ort gepflegt werden.", "Portfolio, Projekt-Detail, Ressourcen, Aufgabensteuerung, Notizen und Projektleiter-Workflows.", "Next.js|TypeScript|Zustand|Tailwind CSS", new DateTime(2026, 3, 23, 9, 59, 54, 364, DateTimeKind.Utc).AddTicks(6719) });

            migrationBuilder.UpdateData(
                table: "Projects",
                keyColumn: "Id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                columns: new[] { "CreatedAt", "Description", "NextMilestone", "TechnologiesCsv", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 23, 9, 59, 54, 364, DateTimeKind.Utc).AddTicks(6737), "Tool fuer schnelle Management-Briefings mit Projektstatus, Risiken und To-dos aus einer Quelle.", "Status- und Risikoantworten auf die neuen Projekte umstellen", "Next.js|TypeScript|LLM Integration|Framer Motion", new DateTime(2026, 3, 23, 9, 59, 54, 364, DateTimeKind.Utc).AddTicks(6737) });

            migrationBuilder.UpdateData(
                table: "ResourceAllocations",
                keyColumn: "Id",
                keyValue: new Guid("12121212-1212-1212-1212-121212121212"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 23, 9, 59, 54, 364, DateTimeKind.Utc).AddTicks(6785), new DateTime(2026, 3, 23, 9, 59, 54, 364, DateTimeKind.Utc).AddTicks(6786) });

            migrationBuilder.UpdateData(
                table: "ResourceAllocations",
                keyColumn: "Id",
                keyValue: new Guid("13131313-1313-1313-1313-131313131313"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 23, 9, 59, 54, 364, DateTimeKind.Utc).AddTicks(6789), new DateTime(2026, 3, 23, 9, 59, 54, 364, DateTimeKind.Utc).AddTicks(6789) });

            migrationBuilder.UpdateData(
                table: "ResourceAllocations",
                keyColumn: "Id",
                keyValue: new Guid("14141414-1414-1414-1414-141414141414"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 23, 9, 59, 54, 364, DateTimeKind.Utc).AddTicks(6791), new DateTime(2026, 3, 23, 9, 59, 54, 364, DateTimeKind.Utc).AddTicks(6792) });

            migrationBuilder.UpdateData(
                table: "ResourceAllocations",
                keyColumn: "Id",
                keyValue: new Guid("15151515-1515-1515-1515-151515151515"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 23, 9, 59, 54, 364, DateTimeKind.Utc).AddTicks(6796), new DateTime(2026, 3, 23, 9, 59, 54, 364, DateTimeKind.Utc).AddTicks(6797) });

            migrationBuilder.UpdateData(
                table: "ResourceAllocations",
                keyColumn: "Id",
                keyValue: new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 23, 9, 59, 54, 364, DateTimeKind.Utc).AddTicks(6768), new DateTime(2026, 3, 23, 9, 59, 54, 364, DateTimeKind.Utc).AddTicks(6768) });

            migrationBuilder.UpdateData(
                table: "ResourceAllocations",
                keyColumn: "Id",
                keyValue: new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 23, 9, 59, 54, 364, DateTimeKind.Utc).AddTicks(6775), new DateTime(2026, 3, 23, 9, 59, 54, 364, DateTimeKind.Utc).AddTicks(6775) });

            migrationBuilder.UpdateData(
                table: "ResourceAllocations",
                keyColumn: "Id",
                keyValue: new Guid("dddddddd-dddd-dddd-dddd-dddddddddddd"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 23, 9, 59, 54, 364, DateTimeKind.Utc).AddTicks(6777), new DateTime(2026, 3, 23, 9, 59, 54, 364, DateTimeKind.Utc).AddTicks(6778) });

            migrationBuilder.UpdateData(
                table: "ResourceAllocations",
                keyColumn: "Id",
                keyValue: new Guid("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 23, 9, 59, 54, 364, DateTimeKind.Utc).AddTicks(6780), new DateTime(2026, 3, 23, 9, 59, 54, 364, DateTimeKind.Utc).AddTicks(6780) });

            migrationBuilder.UpdateData(
                table: "ResourceAllocations",
                keyColumn: "Id",
                keyValue: new Guid("ffffffff-ffff-ffff-ffff-ffffffffffff"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 23, 9, 59, 54, 364, DateTimeKind.Utc).AddTicks(6783), new DateTime(2026, 3, 23, 9, 59, 54, 364, DateTimeKind.Utc).AddTicks(6783) });

            migrationBuilder.UpdateData(
                table: "Tenants",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 23, 9, 59, 54, 364, DateTimeKind.Utc).AddTicks(6468), new DateTime(2026, 3, 23, 9, 59, 54, 364, DateTimeKind.Utc).AddTicks(6472) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 23, 9, 59, 54, 364, DateTimeKind.Utc).AddTicks(6600), new DateTime(2026, 3, 23, 9, 59, 54, 364, DateTimeKind.Utc).AddTicks(6601) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 23, 9, 59, 54, 364, DateTimeKind.Utc).AddTicks(6614), new DateTime(2026, 3, 23, 9, 59, 54, 364, DateTimeKind.Utc).AddTicks(6614) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 23, 9, 59, 54, 364, DateTimeKind.Utc).AddTicks(6616), new DateTime(2026, 3, 23, 9, 59, 54, 364, DateTimeKind.Utc).AddTicks(6617) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("66666666-6666-6666-6666-666666666666"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 23, 9, 59, 54, 364, DateTimeKind.Utc).AddTicks(6619), new DateTime(2026, 3, 23, 9, 59, 54, 364, DateTimeKind.Utc).AddTicks(6619) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("77777777-7777-7777-7777-777777777777"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 23, 9, 59, 54, 364, DateTimeKind.Utc).AddTicks(6621), new DateTime(2026, 3, 23, 9, 59, 54, 364, DateTimeKind.Utc).AddTicks(6621) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("88888888-8888-8888-8888-888888888888"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 23, 9, 59, 54, 364, DateTimeKind.Utc).AddTicks(6623), new DateTime(2026, 3, 23, 9, 59, 54, 364, DateTimeKind.Utc).AddTicks(6624) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("99999999-9999-9999-9999-999999999999"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 23, 9, 59, 54, 364, DateTimeKind.Utc).AddTicks(6626), new DateTime(2026, 3, 23, 9, 59, 54, 364, DateTimeKind.Utc).AddTicks(6626) });

            migrationBuilder.AddForeignKey(
                name: "FK_ActivityLogs_Users_UserId",
                table: "ActivityLogs",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ResourceAllocations_Users_UserId",
                table: "ResourceAllocations",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TaskComments_Users_AuthorId",
                table: "TaskComments",
                column: "AuthorId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Tasks_Users_AssigneeId",
                table: "Tasks",
                column: "AssigneeId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
