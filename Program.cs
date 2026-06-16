using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PmTool.Api.Hubs;
using PmTool.Api.Models;
using PmTool.Api.Services;
using System.Data.Common;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(
        builder.Environment.ContentRootPath,
        ".data-protection-keys")));

var dbProvider = (builder.Configuration["DatabaseProvider"] ?? "sqlite").ToLowerInvariant();
var defaultConn = builder.Configuration.GetConnectionString("Default")
    ?? "Data Source=pmtool.db";

builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (dbProvider == "sqlserver")
    {
        options.UseSqlServer(defaultConn);
        return;
    }

    if (dbProvider == "postgres" || dbProvider == "postgresql")
    {
        options.UseNpgsql(defaultConn);
        return;
    }

    options.UseSqlite(defaultConn);
});

var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? "realcore-pm-secret-2026-heinemann-secure-key!";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer = true,
            ValidIssuer = "pmtool",
            ValidateAudience = true,
            ValidAudience = "pmtool",
            ValidateLifetime = true,
        };

        o.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                var user = context.Principal;
                if (!Guid.TryParse(user?.FindFirstValue("tenantId"), out _) ||
                    !Guid.TryParse(user?.FindFirstValue(ClaimTypes.NameIdentifier), out _))
                {
                    context.Fail("Token is missing required PM Tool claims.");
                }

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddCors(o => o.AddPolicy("AllowAll", p =>
    p.SetIsOriginAllowed(_ => true)
     .AllowAnyMethod()
     .AllowAnyHeader()
     .AllowCredentials())); // AllowCredentials required for SignalR WebSocket

builder.Services.AddHttpClient();
builder.Services.AddControllers();

// ─── SignalR ──────────────────────────────────────────────────────────────────
builder.Services.AddSignalR();

// ─── AI Services ─────────────────────────────────────────────────────────────
builder.Services.AddScoped<AnthropicService>();
builder.Services.AddScoped<EmbeddingService>();
builder.Services.AddScoped<ProjectMatcherService>();
builder.Services.AddScoped<ProjectRetroService>();
builder.Services.AddScoped<CommandService>();

// ─── Background Services ──────────────────────────────────────────────────────
builder.Services.AddHostedService<EmbeddingBackfillService>();
builder.Services.AddHostedService<WebhookRenewalService>();
builder.Services.AddHostedService<MondayBriefingService>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "realcore PM API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new()
    {
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
    c.AddSecurityRequirement(new()
    {
        {
            new()
            {
                Reference = new()
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        InitializeDatabase(db, dbProvider);
        SeedDemoUsers(db);
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"DB migration: {ex.Message}");
    }
}

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "realcore PM API v1");
    c.RoutePrefix = "swagger";
});

app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<PmHub>("/hubs/pm");
app.MapGet("/health", () => Results.Ok(new { status = "healthy", ts = DateTime.UtcNow, provider = dbProvider }));

app.Run();

static void SeedDemoUsers(AppDbContext db)
{
    var demoHash = BCrypt.Net.BCrypt.HashPassword("demo1234");
    foreach (var user in db.Users)
    {
        if (!BCrypt.Net.BCrypt.Verify("demo1234", user.PasswordHash))
        {
            user.PasswordHash = demoHash;
            user.UpdatedAt = DateTime.UtcNow;
        }
    }

    db.SaveChanges();
}

static void InitializeDatabase(AppDbContext db, string dbProvider)
{
    if (dbProvider == "sqlserver")
    {
        InitializeSqlServerDatabase(db);
        return;
    }

    db.Database.Migrate();
}

static void InitializeSqlServerDatabase(AppDbContext db)
{
    if (!TableExists(db, "Projects"))
    {
        db.Database.EnsureCreated();
        return;
    }

    EnsureSqlServerCompatibility(db);
}

static bool TableExists(AppDbContext db, string tableName)
{
    var connection = db.Database.GetDbConnection();
    var shouldClose = connection.State != System.Data.ConnectionState.Open;

    if (shouldClose)
    {
        connection.Open();
    }

    try
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = @tableName";

        var parameter = command.CreateParameter();
        parameter.ParameterName = "@tableName";
        parameter.Value = tableName;
        command.Parameters.Add(parameter);

        var result = command.ExecuteScalar();
        return Convert.ToInt32(result) > 0;
    }
    finally
    {
        if (shouldClose)
        {
            connection.Close();
        }
    }
}

static bool ColumnExists(AppDbContext db, string tableName, string columnName)
{
    var connection = db.Database.GetDbConnection();
    var shouldClose = connection.State != System.Data.ConnectionState.Open;

    if (shouldClose)
    {
        connection.Open();
    }

    try
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @tableName AND COLUMN_NAME = @columnName";

        var tableParameter = command.CreateParameter();
        tableParameter.ParameterName = "@tableName";
        tableParameter.Value = tableName;
        command.Parameters.Add(tableParameter);

        var columnParameter = command.CreateParameter();
        columnParameter.ParameterName = "@columnName";
        columnParameter.Value = columnName;
        command.Parameters.Add(columnParameter);

        var result = command.ExecuteScalar();
        return Convert.ToInt32(result) > 0;
    }
    finally
    {
        if (shouldClose)
        {
            connection.Close();
        }
    }
}

static bool IndexExists(AppDbContext db, string tableName, string indexName)
{
    var connection = db.Database.GetDbConnection();
    var shouldClose = connection.State != System.Data.ConnectionState.Open;

    if (shouldClose)
    {
        connection.Open();
    }

    try
    {
        using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT COUNT(*)
FROM sys.indexes i
INNER JOIN sys.tables t ON i.object_id = t.object_id
WHERE t.name = @tableName AND i.name = @indexName";

        var tableParameter = command.CreateParameter();
        tableParameter.ParameterName = "@tableName";
        tableParameter.Value = tableName;
        command.Parameters.Add(tableParameter);

        var indexParameter = command.CreateParameter();
        indexParameter.ParameterName = "@indexName";
        indexParameter.Value = indexName;
        command.Parameters.Add(indexParameter);

        var result = command.ExecuteScalar();
        return Convert.ToInt32(result) > 0;
    }
    finally
    {
        if (shouldClose)
        {
            connection.Close();
        }
    }
}

static void EnsureColumn(AppDbContext db, string tableName, string columnName, string sqlDefinition)
{
    if (!ColumnExists(db, tableName, columnName))
    {
        db.Database.ExecuteSqlRaw("ALTER TABLE [" + tableName + "] ADD [" + columnName + "] " + sqlDefinition);
    }
}

static void EnsureTable(AppDbContext db, string tableName, string createSql)
{
    if (!TableExists(db, tableName))
    {
        db.Database.ExecuteSqlRaw(createSql);
    }
}

static void EnsureIndex(AppDbContext db, string tableName, string indexName, string createSql)
{
    if (!IndexExists(db, tableName, indexName))
    {
        db.Database.ExecuteSqlRaw(createSql);
    }
}

static void EnsureSqlServerCompatibility(AppDbContext db)
{
    EnsureColumn(db, "Projects", "Category", "nvarchar(50) NOT NULL CONSTRAINT DF_Projects_Category DEFAULT 'delivery'");
    EnsureColumn(db, "Projects", "Stage", "nvarchar(50) NOT NULL CONSTRAINT DF_Projects_Stage DEFAULT 'planning'");
    EnsureColumn(db, "Projects", "DeliveryModel", "nvarchar(100) NOT NULL CONSTRAINT DF_Projects_DeliveryModel DEFAULT ''");
    EnsureColumn(db, "Projects", "Sponsor", "nvarchar(200) NOT NULL CONSTRAINT DF_Projects_Sponsor DEFAULT ''");
    EnsureColumn(db, "Projects", "ExecutiveSummary", "nvarchar(max) NOT NULL CONSTRAINT DF_Projects_ExecutiveSummary DEFAULT ''");
    EnsureColumn(db, "Projects", "HealthSummary", "nvarchar(max) NOT NULL CONSTRAINT DF_Projects_HealthSummary DEFAULT ''");
    EnsureColumn(db, "Projects", "Objective", "nvarchar(max) NOT NULL CONSTRAINT DF_Projects_Objective DEFAULT ''");
    EnsureColumn(db, "Projects", "Scope", "nvarchar(max) NOT NULL CONSTRAINT DF_Projects_Scope DEFAULT ''");
    EnsureColumn(db, "Projects", "SuccessMetric", "nvarchar(max) NOT NULL CONSTRAINT DF_Projects_SuccessMetric DEFAULT ''");
    EnsureColumn(db, "Projects", "Communication", "nvarchar(max) NOT NULL CONSTRAINT DF_Projects_Communication DEFAULT ''");
    EnsureColumn(db, "Projects", "NextMilestone", "nvarchar(max) NOT NULL CONSTRAINT DF_Projects_NextMilestone DEFAULT ''");
    EnsureColumn(db, "Projects", "StakeholdersCsv", "nvarchar(max) NOT NULL CONSTRAINT DF_Projects_StakeholdersCsv DEFAULT ''");
    EnsureColumn(db, "Projects", "TechnologiesCsv", "nvarchar(max) NOT NULL CONSTRAINT DF_Projects_TechnologiesCsv DEFAULT ''");
    EnsureColumn(db, "ResourceAllocations", "ProjectRole", "nvarchar(100) NOT NULL CONSTRAINT DF_ResourceAllocations_ProjectRole DEFAULT ''");
    EnsureColumn(db, "ResourceAllocations", "Responsibility", "nvarchar(max) NOT NULL CONSTRAINT DF_ResourceAllocations_Responsibility DEFAULT ''");
    EnsureColumn(db, "ProjectKnowledgeItems", "Category", "nvarchar(50) NOT NULL CONSTRAINT DF_ProjectKnowledgeItems_Category DEFAULT 'general'");
    EnsureColumn(db, "ProjectKnowledgeItems", "SourceFileName", "nvarchar(260) NOT NULL CONSTRAINT DF_ProjectKnowledgeItems_SourceFileName DEFAULT ''");
    EnsureColumn(db, "ProjectKnowledgeItems", "Version", "int NOT NULL CONSTRAINT DF_ProjectKnowledgeItems_Version DEFAULT 1");
    EnsureColumn(db, "ProjectKnowledgeItems", "ParentKnowledgeItemId", "uniqueidentifier NULL");
    EnsureColumn(db, "ProjectKnowledgeItems", "LinkedEntityType", "nvarchar(50) NOT NULL CONSTRAINT DF_ProjectKnowledgeItems_LinkedEntityType DEFAULT ''");
    EnsureColumn(db, "ProjectKnowledgeItems", "LinkedEntityId", "uniqueidentifier NULL");
    EnsureColumn(db, "ProjectKnowledgeItems", "MeetingReference", "nvarchar(200) NOT NULL CONSTRAINT DF_ProjectKnowledgeItems_MeetingReference DEFAULT ''");

    EnsureTable(db, "ProjectMilestones", @"
CREATE TABLE [ProjectMilestones](
    [Id] uniqueidentifier NOT NULL PRIMARY KEY,
    [ProjectId] uniqueidentifier NOT NULL,
    [OwnerId] uniqueidentifier NOT NULL,
    [Title] nvarchar(200) NOT NULL,
    [Description] nvarchar(max) NOT NULL,
    [DueDate] date NOT NULL,
    [Status] nvarchar(20) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [FK_ProjectMilestones_Projects_ProjectId] FOREIGN KEY ([ProjectId]) REFERENCES [Projects]([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_ProjectMilestones_Users_OwnerId] FOREIGN KEY ([OwnerId]) REFERENCES [Users]([Id])
)");

    EnsureTable(db, "ProjectDecisions", @"
CREATE TABLE [ProjectDecisions](
    [Id] uniqueidentifier NOT NULL PRIMARY KEY,
    [ProjectId] uniqueidentifier NOT NULL,
    [OwnerId] uniqueidentifier NOT NULL,
    [Title] nvarchar(200) NOT NULL,
    [Context] nvarchar(max) NOT NULL,
    [Decision] nvarchar(max) NOT NULL,
    [DueDate] date NOT NULL,
    [Status] nvarchar(20) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [FK_ProjectDecisions_Projects_ProjectId] FOREIGN KEY ([ProjectId]) REFERENCES [Projects]([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_ProjectDecisions_Users_OwnerId] FOREIGN KEY ([OwnerId]) REFERENCES [Users]([Id])
)");

    EnsureTable(db, "ProjectDocuments", @"
CREATE TABLE [ProjectDocuments](
    [Id] uniqueidentifier NOT NULL PRIMARY KEY,
    [ProjectId] uniqueidentifier NOT NULL,
    [OwnerId] uniqueidentifier NOT NULL,
    [Title] nvarchar(200) NOT NULL,
    [Category] nvarchar(100) NOT NULL,
    [Url] nvarchar(500) NOT NULL,
    [Status] nvarchar(20) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [FK_ProjectDocuments_Projects_ProjectId] FOREIGN KEY ([ProjectId]) REFERENCES [Projects]([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_ProjectDocuments_Users_OwnerId] FOREIGN KEY ([OwnerId]) REFERENCES [Users]([Id])
)");

    EnsureTable(db, "ProjectGovernanceChecks", @"
CREATE TABLE [ProjectGovernanceChecks](
    [Id] uniqueidentifier NOT NULL PRIMARY KEY,
    [ProjectId] uniqueidentifier NOT NULL,
    [OwnerId] uniqueidentifier NOT NULL,
    [Title] nvarchar(200) NOT NULL,
    [Area] nvarchar(100) NOT NULL,
    [Notes] nvarchar(max) NOT NULL,
    [DueDate] date NOT NULL,
    [Status] nvarchar(20) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [FK_ProjectGovernanceChecks_Projects_ProjectId] FOREIGN KEY ([ProjectId]) REFERENCES [Projects]([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_ProjectGovernanceChecks_Users_OwnerId] FOREIGN KEY ([OwnerId]) REFERENCES [Users]([Id])
)");

    EnsureTable(db, "ProjectStageGates", @"
CREATE TABLE [ProjectStageGates](
    [Id] uniqueidentifier NOT NULL PRIMARY KEY,
    [ProjectId] uniqueidentifier NOT NULL,
    [OwnerId] uniqueidentifier NOT NULL,
    [Title] nvarchar(200) NOT NULL,
    [StageKey] nvarchar(100) NOT NULL,
    [GateOrder] int NOT NULL,
    [Status] nvarchar(20) NOT NULL,
    [DueDate] date NOT NULL,
    [Notes] nvarchar(max) NOT NULL,
    [ApprovalSummary] nvarchar(max) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [FK_ProjectStageGates_Projects_ProjectId] FOREIGN KEY ([ProjectId]) REFERENCES [Projects]([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_ProjectStageGates_Users_OwnerId] FOREIGN KEY ([OwnerId]) REFERENCES [Users]([Id])
)");

    EnsureTable(db, "ProjectStageGateChecks", @"
CREATE TABLE [ProjectStageGateChecks](
    [Id] uniqueidentifier NOT NULL PRIMARY KEY,
    [StageGateId] uniqueidentifier NOT NULL,
    [Title] nvarchar(200) NOT NULL,
    [RequirementType] nvarchar(100) NOT NULL,
    [Status] nvarchar(20) NOT NULL,
    [IsMandatory] bit NOT NULL,
    [Notes] nvarchar(max) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [FK_ProjectStageGateChecks_ProjectStageGates_StageGateId] FOREIGN KEY ([StageGateId]) REFERENCES [ProjectStageGates]([Id]) ON DELETE CASCADE
)");

    EnsureTable(db, "ProjectApprovals", @"
CREATE TABLE [ProjectApprovals](
    [Id] uniqueidentifier NOT NULL PRIMARY KEY,
    [ProjectId] uniqueidentifier NOT NULL,
    [StageGateId] uniqueidentifier NULL,
    [RequestedById] uniqueidentifier NOT NULL,
    [DecidedById] uniqueidentifier NULL,
    [Title] nvarchar(200) NOT NULL,
    [ApprovalType] nvarchar(100) NOT NULL,
    [Status] nvarchar(20) NOT NULL,
    [DueDate] date NOT NULL,
    [DecisionNotes] nvarchar(max) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [FK_ProjectApprovals_Projects_ProjectId] FOREIGN KEY ([ProjectId]) REFERENCES [Projects]([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_ProjectApprovals_ProjectStageGates_StageGateId] FOREIGN KEY ([StageGateId]) REFERENCES [ProjectStageGates]([Id]),
    CONSTRAINT [FK_ProjectApprovals_Users_RequestedById] FOREIGN KEY ([RequestedById]) REFERENCES [Users]([Id]),
    CONSTRAINT [FK_ProjectApprovals_Users_DecidedById] FOREIGN KEY ([DecidedById]) REFERENCES [Users]([Id])
)");

    EnsureTable(db, "ProjectKnowledgeItems", @"
CREATE TABLE [ProjectKnowledgeItems](
    [Id] uniqueidentifier NOT NULL PRIMARY KEY,
    [ProjectId] uniqueidentifier NOT NULL,
    [AuthorId] uniqueidentifier NOT NULL,
    [Title] nvarchar(200) NOT NULL,
    [SourceType] nvarchar(50) NOT NULL,
    [SourceLabel] nvarchar(500) NOT NULL,
    [Category] nvarchar(50) NOT NULL CONSTRAINT DF_ProjectKnowledgeItems_Category DEFAULT 'general',
    [SourceFileName] nvarchar(260) NOT NULL CONSTRAINT DF_ProjectKnowledgeItems_SourceFileName DEFAULT '',
    [Version] int NOT NULL CONSTRAINT DF_ProjectKnowledgeItems_Version DEFAULT 1,
    [ParentKnowledgeItemId] uniqueidentifier NULL,
    [LinkedEntityType] nvarchar(50) NOT NULL CONSTRAINT DF_ProjectKnowledgeItems_LinkedEntityType DEFAULT '',
    [LinkedEntityId] uniqueidentifier NULL,
    [MeetingReference] nvarchar(200) NOT NULL CONSTRAINT DF_ProjectKnowledgeItems_MeetingReference DEFAULT '',
    [Content] nvarchar(max) NOT NULL,
    [TagsCsv] nvarchar(max) NOT NULL,
    [Importance] int NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [FK_ProjectKnowledgeItems_Projects_ProjectId] FOREIGN KEY ([ProjectId]) REFERENCES [Projects]([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_ProjectKnowledgeItems_Users_AuthorId] FOREIGN KEY ([AuthorId]) REFERENCES [Users]([Id])
)");

    EnsureTable(db, "AiSuggestionFeedback", @"
CREATE TABLE [AiSuggestionFeedback](
    [Id] uniqueidentifier NOT NULL PRIMARY KEY,
    [ProjectId] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [SuggestionType] nvarchar(50) NOT NULL,
    [SuggestionTitle] nvarchar(200) NOT NULL,
    [Status] nvarchar(20) NOT NULL,
    [Notes] nvarchar(max) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [FK_AiSuggestionFeedback_Projects_ProjectId] FOREIGN KEY ([ProjectId]) REFERENCES [Projects]([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_AiSuggestionFeedback_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users]([Id])
)");

    EnsureTable(db, "AuditEntries", @"
CREATE TABLE [AuditEntries](
    [Id] uniqueidentifier NOT NULL PRIMARY KEY,
    [TenantId] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [ProjectId] uniqueidentifier NULL,
    [EntityId] uniqueidentifier NULL,
    [EntityType] nvarchar(50) NOT NULL,
    [ChangeType] nvarchar(50) NOT NULL,
    [Title] nvarchar(200) NOT NULL,
    [FromValue] nvarchar(500) NOT NULL,
    [ToValue] nvarchar(500) NOT NULL,
    [Detail] nvarchar(max) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [FK_AuditEntries_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants]([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_AuditEntries_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users]([Id]),
    CONSTRAINT [FK_AuditEntries_Projects_ProjectId] FOREIGN KEY ([ProjectId]) REFERENCES [Projects]([Id])
)");

    EnsureTable(db, "ProjectForecastSnapshots", @"
CREATE TABLE [ProjectForecastSnapshots](
    [Id] uniqueidentifier NOT NULL PRIMARY KEY,
    [ProjectId] uniqueidentifier NOT NULL,
    [SnapshotDate] date NOT NULL,
    [BudgetAtCompletion] decimal(18,2) NOT NULL,
    [ActualCost] decimal(18,2) NOT NULL,
    [EarnedValue] decimal(18,2) NOT NULL,
    [PlannedValue] decimal(18,2) NOT NULL,
    [EstimateAtCompletion] decimal(18,2) NOT NULL,
    [EstimateToComplete] decimal(18,2) NOT NULL,
    [CostPerformanceIndex] decimal(18,4) NOT NULL,
    [SchedulePerformanceIndex] decimal(18,4) NOT NULL,
    [TotalEstimatedHours] int NOT NULL,
    [LoggedHours] int NOT NULL,
    [RemainingHours] int NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [FK_ProjectForecastSnapshots_Projects_ProjectId] FOREIGN KEY ([ProjectId]) REFERENCES [Projects]([Id]) ON DELETE CASCADE
)");

    EnsureTable(db, "ProjectTeamsLinks", @"
CREATE TABLE [ProjectTeamsLinks](
    [Id] uniqueidentifier NOT NULL PRIMARY KEY,
    [ProjectId] uniqueidentifier NOT NULL,
    [TeamName] nvarchar(200) NOT NULL,
    [ChannelName] nvarchar(200) NOT NULL,
    [TeamId] nvarchar(200) NOT NULL,
    [ChannelId] nvarchar(200) NOT NULL,
    [TenantDomain] nvarchar(200) NOT NULL,
    [SyncStatus] nvarchar(20) NOT NULL,
    [LastSyncAt] datetime2 NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [FK_ProjectTeamsLinks_Projects_ProjectId] FOREIGN KEY ([ProjectId]) REFERENCES [Projects]([Id]) ON DELETE CASCADE
)");

    EnsureTable(db, "ProjectJiraLinks", @"
CREATE TABLE [ProjectJiraLinks](
    [Id] uniqueidentifier NOT NULL PRIMARY KEY,
    [ProjectId] uniqueidentifier NOT NULL,
    [BoardName] nvarchar(200) NOT NULL,
    [ProjectKey] nvarchar(100) NOT NULL,
    [BoardId] nvarchar(100) NOT NULL,
    [JqlFilter] nvarchar(1000) NOT NULL,
    [SyncStatus] nvarchar(20) NOT NULL,
    [LastSyncAt] datetime2 NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [FK_ProjectJiraLinks_Projects_ProjectId] FOREIGN KEY ([ProjectId]) REFERENCES [Projects]([Id]) ON DELETE CASCADE
)");

    EnsureIndex(db, "ProjectMilestones", "IX_ProjectMilestones_ProjectId", "CREATE INDEX [IX_ProjectMilestones_ProjectId] ON [ProjectMilestones]([ProjectId])");
    EnsureIndex(db, "ProjectMilestones", "IX_ProjectMilestones_OwnerId", "CREATE INDEX [IX_ProjectMilestones_OwnerId] ON [ProjectMilestones]([OwnerId])");
    EnsureIndex(db, "ProjectDecisions", "IX_ProjectDecisions_ProjectId", "CREATE INDEX [IX_ProjectDecisions_ProjectId] ON [ProjectDecisions]([ProjectId])");
    EnsureIndex(db, "ProjectDecisions", "IX_ProjectDecisions_OwnerId", "CREATE INDEX [IX_ProjectDecisions_OwnerId] ON [ProjectDecisions]([OwnerId])");
    EnsureIndex(db, "ProjectDocuments", "IX_ProjectDocuments_ProjectId", "CREATE INDEX [IX_ProjectDocuments_ProjectId] ON [ProjectDocuments]([ProjectId])");
    EnsureIndex(db, "ProjectDocuments", "IX_ProjectDocuments_OwnerId", "CREATE INDEX [IX_ProjectDocuments_OwnerId] ON [ProjectDocuments]([OwnerId])");
    EnsureIndex(db, "ProjectGovernanceChecks", "IX_ProjectGovernanceChecks_ProjectId", "CREATE INDEX [IX_ProjectGovernanceChecks_ProjectId] ON [ProjectGovernanceChecks]([ProjectId])");
    EnsureIndex(db, "ProjectGovernanceChecks", "IX_ProjectGovernanceChecks_OwnerId", "CREATE INDEX [IX_ProjectGovernanceChecks_OwnerId] ON [ProjectGovernanceChecks]([OwnerId])");
    EnsureIndex(db, "ProjectStageGates", "IX_ProjectStageGates_ProjectId", "CREATE INDEX [IX_ProjectStageGates_ProjectId] ON [ProjectStageGates]([ProjectId])");
    EnsureIndex(db, "ProjectStageGates", "IX_ProjectStageGates_OwnerId", "CREATE INDEX [IX_ProjectStageGates_OwnerId] ON [ProjectStageGates]([OwnerId])");
    EnsureIndex(db, "ProjectStageGateChecks", "IX_ProjectStageGateChecks_StageGateId", "CREATE INDEX [IX_ProjectStageGateChecks_StageGateId] ON [ProjectStageGateChecks]([StageGateId])");
    EnsureIndex(db, "ProjectApprovals", "IX_ProjectApprovals_ProjectId", "CREATE INDEX [IX_ProjectApprovals_ProjectId] ON [ProjectApprovals]([ProjectId])");
    EnsureIndex(db, "ProjectApprovals", "IX_ProjectApprovals_StageGateId", "CREATE INDEX [IX_ProjectApprovals_StageGateId] ON [ProjectApprovals]([StageGateId])");
    EnsureIndex(db, "ProjectApprovals", "IX_ProjectApprovals_RequestedById", "CREATE INDEX [IX_ProjectApprovals_RequestedById] ON [ProjectApprovals]([RequestedById])");
    EnsureIndex(db, "ProjectKnowledgeItems", "IX_ProjectKnowledgeItems_ProjectId", "CREATE INDEX [IX_ProjectKnowledgeItems_ProjectId] ON [ProjectKnowledgeItems]([ProjectId])");
    EnsureIndex(db, "ProjectKnowledgeItems", "IX_ProjectKnowledgeItems_AuthorId", "CREATE INDEX [IX_ProjectKnowledgeItems_AuthorId] ON [ProjectKnowledgeItems]([AuthorId])");
    EnsureIndex(db, "AiSuggestionFeedback", "IX_AiSuggestionFeedback_ProjectId", "CREATE INDEX [IX_AiSuggestionFeedback_ProjectId] ON [AiSuggestionFeedback]([ProjectId])");
    EnsureIndex(db, "AiSuggestionFeedback", "IX_AiSuggestionFeedback_UserId", "CREATE INDEX [IX_AiSuggestionFeedback_UserId] ON [AiSuggestionFeedback]([UserId])");
    EnsureIndex(db, "AuditEntries", "IX_AuditEntries_TenantId", "CREATE INDEX [IX_AuditEntries_TenantId] ON [AuditEntries]([TenantId])");
    EnsureIndex(db, "AuditEntries", "IX_AuditEntries_ProjectId", "CREATE INDEX [IX_AuditEntries_ProjectId] ON [AuditEntries]([ProjectId])");
    EnsureIndex(db, "ProjectForecastSnapshots", "IX_ProjectForecastSnapshots_ProjectId_SnapshotDate", "CREATE UNIQUE INDEX [IX_ProjectForecastSnapshots_ProjectId_SnapshotDate] ON [ProjectForecastSnapshots]([ProjectId], [SnapshotDate])");
    EnsureIndex(db, "ProjectTeamsLinks", "IX_ProjectTeamsLinks_ProjectId", "CREATE UNIQUE INDEX [IX_ProjectTeamsLinks_ProjectId] ON [ProjectTeamsLinks]([ProjectId])");
    EnsureIndex(db, "ProjectJiraLinks", "IX_ProjectJiraLinks_ProjectId", "CREATE UNIQUE INDEX [IX_ProjectJiraLinks_ProjectId] ON [ProjectJiraLinks]([ProjectId])");

    // AI / Embedding tables
    EnsureTable(db, "EmbeddingVectors", @"
CREATE TABLE [EmbeddingVectors](
    [Id] uniqueidentifier NOT NULL PRIMARY KEY,
    [TenantId] uniqueidentifier NOT NULL,
    [ProjectId] uniqueidentifier NOT NULL,
    [EntityType] nvarchar(50) NOT NULL,
    [EntityId] uniqueidentifier NOT NULL,
    [EmbeddedText] nvarchar(max) NOT NULL,
    [EmbeddingJson] nvarchar(max) NOT NULL,
    [IsInstitutionalMemory] bit NOT NULL DEFAULT 0,
    [Importance] int NOT NULL DEFAULT 3,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [FK_EmbeddingVectors_Projects_ProjectId] FOREIGN KEY ([ProjectId]) REFERENCES [Projects]([Id]) ON DELETE CASCADE
)");

    EnsureTable(db, "GraphWebhookSubscriptions", @"
CREATE TABLE [GraphWebhookSubscriptions](
    [Id] uniqueidentifier NOT NULL PRIMARY KEY,
    [TenantId] uniqueidentifier NOT NULL,
    [SubscriptionId] nvarchar(200) NOT NULL,
    [Resource] nvarchar(200) NOT NULL,
    [ChangeType] nvarchar(50) NOT NULL,
    [ExpirationDateTime] datetime2 NOT NULL,
    [Status] nvarchar(20) NOT NULL,
    [LastRenewedAt] datetime2 NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL
)");

    EnsureTable(db, "ProjectRetroSummaries", @"
CREATE TABLE [ProjectRetroSummaries](
    [Id] uniqueidentifier NOT NULL PRIMARY KEY,
    [TenantId] uniqueidentifier NOT NULL,
    [ProjectId] uniqueidentifier NOT NULL,
    [Summary] nvarchar(max) NOT NULL,
    [LessonsLearned] nvarchar(max) NOT NULL,
    [SuccessFactors] nvarchar(max) NOT NULL,
    [RiskPatterns] nvarchar(max) NOT NULL,
    [ProjectCategory] nvarchar(50) NOT NULL,
    [ProjectStage] nvarchar(50) NOT NULL,
    [DurationDays] int NOT NULL,
    [BudgetVariancePercent] decimal(18,2) NOT NULL,
    [WasDelayed] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [FK_ProjectRetroSummaries_Projects_ProjectId] FOREIGN KEY ([ProjectId]) REFERENCES [Projects]([Id]) ON DELETE CASCADE
)");

    EnsureTable(db, "AiConversations", @"
CREATE TABLE [AiConversations](
    [Id] uniqueidentifier NOT NULL PRIMARY KEY,
    [TenantId] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [ProjectId] uniqueidentifier NULL,
    [Role] nvarchar(20) NOT NULL,
    [Content] nvarchar(max) NOT NULL,
    [ConversationId] nvarchar(50) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [FK_AiConversations_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users]([Id]),
    CONSTRAINT [FK_AiConversations_Projects_ProjectId] FOREIGN KEY ([ProjectId]) REFERENCES [Projects]([Id]) ON DELETE CASCADE
)");

    EnsureIndex(db, "EmbeddingVectors", "IX_EmbeddingVectors_TenantId_ProjectId", "CREATE INDEX [IX_EmbeddingVectors_TenantId_ProjectId] ON [EmbeddingVectors]([TenantId], [ProjectId])");
    EnsureIndex(db, "EmbeddingVectors", "IX_EmbeddingVectors_EntityType_EntityId", "CREATE INDEX [IX_EmbeddingVectors_EntityType_EntityId] ON [EmbeddingVectors]([EntityType], [EntityId])");
    EnsureIndex(db, "GraphWebhookSubscriptions", "IX_GraphWebhookSubscriptions_SubscriptionId", "CREATE UNIQUE INDEX [IX_GraphWebhookSubscriptions_SubscriptionId] ON [GraphWebhookSubscriptions]([SubscriptionId])");
    EnsureIndex(db, "AiConversations", "IX_AiConversations_TenantId_UserId_ConversationId", "CREATE INDEX [IX_AiConversations_TenantId_UserId_ConversationId] ON [AiConversations]([TenantId], [UserId], [ConversationId])");
}
