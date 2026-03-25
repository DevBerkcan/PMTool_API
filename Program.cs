using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PmTool.Api.Models;
using System.Data.Common;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

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
    .AddJwtBearer(o => o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        ValidateIssuer = true,
        ValidIssuer = "pmtool",
        ValidateAudience = true,
        ValidAudience = "pmtool",
        ValidateLifetime = true,
    });

builder.Services.AddAuthorization();
builder.Services.AddCors(o => o.AddPolicy("AllowAll", p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));
builder.Services.AddHttpClient();
builder.Services.AddControllers();
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
        var log = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        log.LogWarning("DB migration: {Msg}", ex.Message);
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

    EnsureTable(db, "ProjectKnowledgeItems", @"
CREATE TABLE [ProjectKnowledgeItems](
    [Id] uniqueidentifier NOT NULL PRIMARY KEY,
    [ProjectId] uniqueidentifier NOT NULL,
    [AuthorId] uniqueidentifier NOT NULL,
    [Title] nvarchar(200) NOT NULL,
    [SourceType] nvarchar(50) NOT NULL,
    [SourceLabel] nvarchar(500) NOT NULL,
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

    EnsureIndex(db, "ProjectMilestones", "IX_ProjectMilestones_ProjectId", "CREATE INDEX [IX_ProjectMilestones_ProjectId] ON [ProjectMilestones]([ProjectId])");
    EnsureIndex(db, "ProjectMilestones", "IX_ProjectMilestones_OwnerId", "CREATE INDEX [IX_ProjectMilestones_OwnerId] ON [ProjectMilestones]([OwnerId])");
    EnsureIndex(db, "ProjectDecisions", "IX_ProjectDecisions_ProjectId", "CREATE INDEX [IX_ProjectDecisions_ProjectId] ON [ProjectDecisions]([ProjectId])");
    EnsureIndex(db, "ProjectDecisions", "IX_ProjectDecisions_OwnerId", "CREATE INDEX [IX_ProjectDecisions_OwnerId] ON [ProjectDecisions]([OwnerId])");
    EnsureIndex(db, "ProjectDocuments", "IX_ProjectDocuments_ProjectId", "CREATE INDEX [IX_ProjectDocuments_ProjectId] ON [ProjectDocuments]([ProjectId])");
    EnsureIndex(db, "ProjectDocuments", "IX_ProjectDocuments_OwnerId", "CREATE INDEX [IX_ProjectDocuments_OwnerId] ON [ProjectDocuments]([OwnerId])");
    EnsureIndex(db, "ProjectGovernanceChecks", "IX_ProjectGovernanceChecks_ProjectId", "CREATE INDEX [IX_ProjectGovernanceChecks_ProjectId] ON [ProjectGovernanceChecks]([ProjectId])");
    EnsureIndex(db, "ProjectGovernanceChecks", "IX_ProjectGovernanceChecks_OwnerId", "CREATE INDEX [IX_ProjectGovernanceChecks_OwnerId] ON [ProjectGovernanceChecks]([OwnerId])");
    EnsureIndex(db, "ProjectKnowledgeItems", "IX_ProjectKnowledgeItems_ProjectId", "CREATE INDEX [IX_ProjectKnowledgeItems_ProjectId] ON [ProjectKnowledgeItems]([ProjectId])");
    EnsureIndex(db, "ProjectKnowledgeItems", "IX_ProjectKnowledgeItems_AuthorId", "CREATE INDEX [IX_ProjectKnowledgeItems_AuthorId] ON [ProjectKnowledgeItems]([AuthorId])");
    EnsureIndex(db, "AiSuggestionFeedback", "IX_AiSuggestionFeedback_ProjectId", "CREATE INDEX [IX_AiSuggestionFeedback_ProjectId] ON [AiSuggestionFeedback]([ProjectId])");
    EnsureIndex(db, "AiSuggestionFeedback", "IX_AiSuggestionFeedback_UserId", "CREATE INDEX [IX_AiSuggestionFeedback_UserId] ON [AiSuggestionFeedback]([UserId])");
    EnsureIndex(db, "ProjectTeamsLinks", "IX_ProjectTeamsLinks_ProjectId", "CREATE UNIQUE INDEX [IX_ProjectTeamsLinks_ProjectId] ON [ProjectTeamsLinks]([ProjectId])");
}
