using Microsoft.EntityFrameworkCore;

namespace PmTool.Api.Models;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectTask> Tasks => Set<ProjectTask>();
    public DbSet<TaskComment> TaskComments => Set<TaskComment>();
    public DbSet<Risk> Risks => Set<Risk>();
    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();
    public DbSet<ResourceAllocation> ResourceAllocations => Set<ResourceAllocation>();
    public DbSet<ProjectNote> ProjectNotes => Set<ProjectNote>();
    public DbSet<ProjectLeadTask> ProjectLeadTasks => Set<ProjectLeadTask>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        base.OnModelCreating(mb);

        mb.Entity<Project>().HasOne(p => p.Owner).WithMany().HasForeignKey(p => p.OwnerId).OnDelete(DeleteBehavior.Restrict);
        mb.Entity<ProjectTask>().HasOne(t => t.Assignee).WithMany().HasForeignKey(t => t.AssigneeId).OnDelete(DeleteBehavior.Restrict);
        mb.Entity<Risk>().HasOne(r => r.Owner).WithMany().HasForeignKey(r => r.OwnerId).OnDelete(DeleteBehavior.Restrict);
        mb.Entity<ActivityLog>().HasOne(a => a.User).WithMany().HasForeignKey(a => a.UserId).OnDelete(DeleteBehavior.Restrict);
        mb.Entity<TaskComment>().HasOne(c => c.Author).WithMany().HasForeignKey(c => c.AuthorId).OnDelete(DeleteBehavior.Restrict);
        mb.Entity<ResourceAllocation>().HasOne(r => r.User).WithMany().HasForeignKey(r => r.UserId).OnDelete(DeleteBehavior.Restrict);
        mb.Entity<ResourceAllocation>().HasOne(r => r.Project).WithMany(p => p.TeamAssignments).HasForeignKey(r => r.ProjectId).OnDelete(DeleteBehavior.Cascade);
        mb.Entity<ProjectNote>().HasOne(n => n.Project).WithMany(p => p.Notes).HasForeignKey(n => n.ProjectId).OnDelete(DeleteBehavior.Cascade);
        mb.Entity<ProjectNote>().HasOne(n => n.Author).WithMany().HasForeignKey(n => n.AuthorId).OnDelete(DeleteBehavior.Restrict);
        mb.Entity<ProjectLeadTask>().HasOne(t => t.Project).WithMany(p => p.LeadTasks).HasForeignKey(t => t.ProjectId).OnDelete(DeleteBehavior.Cascade);
        mb.Entity<ProjectLeadTask>().HasOne(t => t.Owner).WithMany().HasForeignKey(t => t.OwnerId).OnDelete(DeleteBehavior.Restrict);
        mb.Entity<Project>().HasIndex(p => p.TenantId);
        mb.Entity<ProjectTask>().HasIndex(t => t.ProjectId);
        mb.Entity<ResourceAllocation>().HasIndex(r => new { r.UserId, r.ProjectId }).IsUnique();

        var tenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var ownerId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var selinId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var emreId = Guid.Parse("55555555-5555-5555-5555-555555555555");
        var miraId = Guid.Parse("66666666-6666-6666-6666-666666666666");
        var canId = Guid.Parse("77777777-7777-7777-7777-777777777777");
        var lenaId = Guid.Parse("88888888-8888-8888-8888-888888888888");
        var jonasId = Guid.Parse("99999999-9999-9999-9999-999999999999");

        var gShareId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var briefingId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

        const string demoHash = "$2a$11$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2uheWG/igi.";

        mb.Entity<Tenant>().HasData(new Tenant { Id = tenantId, Name = "RealCore", Slug = "realcore" });
        mb.Entity<User>().HasData(
            new User { Id = ownerId, TenantId = tenantId, DisplayName = "Berk Can Atesoglu", Email = "berkcan@realcore.de", PasswordHash = demoHash, Role = "Projektleiter" },
            new User { Id = selinId, TenantId = tenantId, DisplayName = "Selin Kaya", Email = "selin@realcore.de", PasswordHash = demoHash, Role = "Product Owner" },
            new User { Id = emreId, TenantId = tenantId, DisplayName = "Emre Yilmaz", Email = "emre@realcore.de", PasswordHash = demoHash, Role = "Frontend Developer" },
            new User { Id = miraId, TenantId = tenantId, DisplayName = "Mira Hoffmann", Email = "mira@realcore.de", PasswordHash = demoHash, Role = "Backend Developer" },
            new User { Id = canId, TenantId = tenantId, DisplayName = "Can Demir", Email = "can@realcore.de", PasswordHash = demoHash, Role = "AI Engineer" },
            new User { Id = lenaId, TenantId = tenantId, DisplayName = "Lena Schmidt", Email = "lena@realcore.de", PasswordHash = demoHash, Role = "UX Designer" },
            new User { Id = jonasId, TenantId = tenantId, DisplayName = "Jonas Weber", Email = "jonas@realcore.de", PasswordHash = demoHash, Role = "QA Engineer" }
        );

        mb.Entity<Project>().HasData(
            new Project
            {
                Id = gShareId,
                TenantId = tenantId,
                OwnerId = ownerId,
                Name = "G-Share",
                Description = "Zentrale Plattform fuer Projektsteuerung, Teamtransparenz und operative Zusammenarbeit.",
                Customer = "RealCore Intern",
                Objective = "Ein zentrales Tool schaffen, in dem Projekte, Teams, Aufgaben und Notizen an einem Ort gepflegt werden.",
                Scope = "Portfolio, Projekt-Detail, Ressourcen, Aufgabensteuerung, Notizen und Projektleiter-Workflows.",
                SuccessMetric = "Projektteams koennen alle relevanten Projektinformationen in unter 2 Minuten finden und pflegen.",
                Communication = "Wochenstatus montags, Team-Sync mittwochs, Review freitags.",
                NextMilestone = "Projekt-Detailseite mit Team und Notizen live schalten",
                StakeholdersCsv = "Management|Projektleitung|Delivery Team",
                TechnologiesCsv = "Next.js|TypeScript|Zustand|Tailwind CSS",
                Status = "green",
                ProgressPercent = 72,
                BudgetTotal = 180000,
                BudgetSpent = 126000,
                StartDate = new DateOnly(2026, 1, 15),
                EndDate = new DateOnly(2026, 6, 30)
            },
            new Project
            {
                Id = briefingId,
                TenantId = tenantId,
                OwnerId = ownerId,
                Name = "AI Briefing Tool",
                Description = "Tool fuer schnelle Management-Briefings mit Projektstatus, Risiken und To-dos aus einer Quelle.",
                Customer = "RealCore Intern",
                Objective = "Management-Briefings fuer Projekte automatisiert, kompakt und nachvollziehbar bereitstellen.",
                Scope = "KI-Assistenz, Projektabfragen, Priorisierung von Risiken und Management-Zusammenfassungen.",
                SuccessMetric = "Ein Briefing fuer ein Projekt kann in unter 30 Sekunden erstellt werden.",
                Communication = "Briefing Review dienstags, Prompt-Tuning donnerstags.",
                NextMilestone = "Status- und Risikoantworten auf die neuen Projekte umstellen",
                StakeholdersCsv = "Management|Sales|Projektleitung",
                TechnologiesCsv = "Next.js|TypeScript|LLM Integration|Framer Motion",
                Status = "yellow",
                ProgressPercent = 48,
                BudgetTotal = 140000,
                BudgetSpent = 76000,
                StartDate = new DateOnly(2026, 2, 1),
                EndDate = new DateOnly(2026, 7, 15)
            }
        );

        mb.Entity<ResourceAllocation>().HasData(
            new ResourceAllocation { Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), UserId = ownerId, ProjectId = gShareId, AllocatedHours = 34, ProjectRole = "Projektleiter", Responsibility = "Steuerung, Stakeholder-Management, Priorisierung" },
            new ResourceAllocation { Id = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"), UserId = selinId, ProjectId = gShareId, AllocatedHours = 30, ProjectRole = "Product Owner", Responsibility = "Anforderungen, Backlog, Fachseite" },
            new ResourceAllocation { Id = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"), UserId = emreId, ProjectId = gShareId, AllocatedHours = 36, ProjectRole = "Frontend Developer", Responsibility = "Dashboard, Projektseiten, UX-Umsetzung" },
            new ResourceAllocation { Id = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"), UserId = miraId, ProjectId = gShareId, AllocatedHours = 32, ProjectRole = "Backend Developer", Responsibility = "APIs, Datenmodell, Berechtigungen" },
            new ResourceAllocation { Id = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff"), UserId = jonasId, ProjectId = gShareId, AllocatedHours = 28, ProjectRole = "QA Engineer", Responsibility = "Testfaelle, Regression, Abnahme" },
            new ResourceAllocation { Id = Guid.Parse("12121212-1212-1212-1212-121212121212"), UserId = ownerId, ProjectId = briefingId, AllocatedHours = 18, ProjectRole = "Projektleiter", Responsibility = "Roadmap, Stakeholder, Freigaben" },
            new ResourceAllocation { Id = Guid.Parse("13131313-1313-1313-1313-131313131313"), UserId = canId, ProjectId = briefingId, AllocatedHours = 38, ProjectRole = "AI Engineer", Responsibility = "Prompting, Auswertung, Response-Logik" },
            new ResourceAllocation { Id = Guid.Parse("14141414-1414-1414-1414-141414141414"), UserId = emreId, ProjectId = briefingId, AllocatedHours = 20, ProjectRole = "Frontend Developer", Responsibility = "Chat-UI und Briefing-Darstellung" },
            new ResourceAllocation { Id = Guid.Parse("15151515-1515-1515-1515-151515151515"), UserId = lenaId, ProjectId = briefingId, AllocatedHours = 24, ProjectRole = "UX Designer", Responsibility = "Informationsarchitektur und Lesbarkeit" }
        );

        mb.Entity<ProjectNote>().HasData(
            new ProjectNote { Id = Guid.Parse("16161616-1616-1616-1616-161616161616"), ProjectId = gShareId, AuthorId = ownerId, Title = "Kickoff Ergebnis", Content = "Projektstruktur, Rollen und erste Prioritaeten mit dem Team abgestimmt.", CreatedAt = new DateTime(2026, 2, 2, 9, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2026, 2, 2, 9, 0, 0, DateTimeKind.Utc) },
            new ProjectNote { Id = Guid.Parse("17171717-1717-1717-1717-171717171717"), ProjectId = gShareId, AuthorId = selinId, Title = "Nutzerfeedback", Content = "Projektleiter wollen Teammitglieder direkt am Projekt sehen und Notizen im Projekt pflegen.", CreatedAt = new DateTime(2026, 3, 18, 13, 30, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2026, 3, 18, 13, 30, 0, DateTimeKind.Utc) },
            new ProjectNote { Id = Guid.Parse("18181818-1818-1818-1818-181818181818"), ProjectId = briefingId, AuthorId = ownerId, Title = "Scope Fokus", Content = "Zunaechst nur zwei Projekte unterstuetzen, um das Briefing konsistent zu halten.", CreatedAt = new DateTime(2026, 3, 10, 10, 15, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2026, 3, 10, 10, 15, 0, DateTimeKind.Utc) }
        );

        mb.Entity<ProjectLeadTask>().HasData(
            new ProjectLeadTask { Id = Guid.Parse("19191919-1919-1919-1919-191919191919"), ProjectId = gShareId, OwnerId = ownerId, Title = "Wochenstatus vorbereiten", Description = "Budget, Fortschritt und Risiken fuer den Wochenbericht aktualisieren.", DueDate = new DateOnly(2026, 3, 25), Status = "in_progress" },
            new ProjectLeadTask { Id = Guid.Parse("20202020-2020-2020-2020-202020202020"), ProjectId = gShareId, OwnerId = ownerId, Title = "Abnahme mit Stakeholdern planen", Description = "Termin fuer Review der Projekt-Detailseiten festlegen.", DueDate = new DateOnly(2026, 3, 29), Status = "todo" },
            new ProjectLeadTask { Id = Guid.Parse("21212121-2121-2121-2121-212121212121"), ProjectId = gShareId, OwnerId = ownerId, Title = "Offene Entscheidungen dokumentieren", Description = "Offene Scope-Fragen in den Projektnotizen festhalten.", DueDate = new DateOnly(2026, 3, 22), Status = "done" },
            new ProjectLeadTask { Id = Guid.Parse("22222221-2222-2222-2222-222222222221"), ProjectId = briefingId, OwnerId = ownerId, Title = "Prompt-Vorlagen abstimmen", Description = "Formulierung fuer Status- und Risikoantworten finalisieren.", DueDate = new DateOnly(2026, 3, 26), Status = "todo" },
            new ProjectLeadTask { Id = Guid.Parse("23232323-2323-2323-2323-232323232323"), ProjectId = briefingId, OwnerId = ownerId, Title = "Testbriefing mit Management teilen", Description = "Feedback fuer die erste Briefing-Version einholen.", DueDate = new DateOnly(2026, 3, 30), Status = "in_progress" }
        );
    }
}
