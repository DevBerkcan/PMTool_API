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
    if (HasRequiredSqlServerSchema(db))
    {
        return;
    }

    DropAppTables(db);

    db.Database.EnsureCreated();
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

static void DropAppTables(AppDbContext db)
{
    var tables = new[]
    {
        "TaskComments",
        "Tasks",
        "ProjectMilestones",
        "ProjectDecisions",
        "ProjectDocuments",
        "ProjectGovernanceChecks",
        "ProjectKnowledgeItems",
        "AiSuggestionFeedback",
        "ProjectLeadTasks",
        "ProjectNotes",
        "ActivityLogs",
        "Risks",
        "ResourceAllocations",
        "Projects",
        "Users",
        "Tenants",
        "__EFMigrationsHistory"
    };

    foreach (var table in tables)
    {
        if (!TableExists(db, table))
        {
            continue;
        }

        db.Database.ExecuteSqlRaw("DROP TABLE [" + table + "]");
    }
}

static bool HasRequiredSqlServerSchema(AppDbContext db)
{
    var requiredTables = new[]
    {
        "Tenants",
        "Users",
        "Projects",
        "ResourceAllocations",
        "Tasks",
        "TaskComments",
        "Risks",
        "ActivityLogs",
        "ProjectNotes",
        "ProjectLeadTasks",
        "ProjectMilestones",
        "ProjectDecisions",
        "ProjectDocuments",
        "ProjectGovernanceChecks",
        "ProjectKnowledgeItems",
        "AiSuggestionFeedback"
    };

    foreach (var table in requiredTables)
    {
        if (!TableExists(db, table))
        {
            return false;
        }
    }

    return true;
}
