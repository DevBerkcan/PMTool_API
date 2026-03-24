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
    public DbSet<ProjectMilestone> ProjectMilestones => Set<ProjectMilestone>();
    public DbSet<ProjectDecision> ProjectDecisions => Set<ProjectDecision>();
    public DbSet<ProjectDocument> ProjectDocuments => Set<ProjectDocument>();
    public DbSet<ProjectGovernanceCheck> ProjectGovernanceChecks => Set<ProjectGovernanceCheck>();
    public DbSet<ProjectKnowledgeItem> ProjectKnowledgeItems => Set<ProjectKnowledgeItem>();
    public DbSet<AiSuggestionFeedback> AiSuggestionFeedback => Set<AiSuggestionFeedback>();
    public DbSet<ProjectTeamsLink> ProjectTeamsLinks => Set<ProjectTeamsLink>();

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
        mb.Entity<ProjectMilestone>().HasOne(t => t.Project).WithMany(p => p.Milestones).HasForeignKey(t => t.ProjectId).OnDelete(DeleteBehavior.Cascade);
        mb.Entity<ProjectMilestone>().HasOne(t => t.Owner).WithMany().HasForeignKey(t => t.OwnerId).OnDelete(DeleteBehavior.Restrict);
        mb.Entity<ProjectDecision>().HasOne(t => t.Project).WithMany(p => p.Decisions).HasForeignKey(t => t.ProjectId).OnDelete(DeleteBehavior.Cascade);
        mb.Entity<ProjectDecision>().HasOne(t => t.Owner).WithMany().HasForeignKey(t => t.OwnerId).OnDelete(DeleteBehavior.Restrict);
        mb.Entity<ProjectDocument>().HasOne(t => t.Project).WithMany(p => p.Documents).HasForeignKey(t => t.ProjectId).OnDelete(DeleteBehavior.Cascade);
        mb.Entity<ProjectDocument>().HasOne(t => t.Owner).WithMany().HasForeignKey(t => t.OwnerId).OnDelete(DeleteBehavior.Restrict);
        mb.Entity<ProjectGovernanceCheck>().HasOne(t => t.Project).WithMany(p => p.GovernanceChecks).HasForeignKey(t => t.ProjectId).OnDelete(DeleteBehavior.Cascade);
        mb.Entity<ProjectGovernanceCheck>().HasOne(t => t.Owner).WithMany().HasForeignKey(t => t.OwnerId).OnDelete(DeleteBehavior.Restrict);
        mb.Entity<ProjectKnowledgeItem>().HasOne(t => t.Project).WithMany(p => p.KnowledgeItems).HasForeignKey(t => t.ProjectId).OnDelete(DeleteBehavior.Cascade);
        mb.Entity<ProjectKnowledgeItem>().HasOne(t => t.Author).WithMany().HasForeignKey(t => t.AuthorId).OnDelete(DeleteBehavior.Restrict);
        mb.Entity<AiSuggestionFeedback>().HasOne(t => t.Project).WithMany(p => p.AiSuggestionFeedback).HasForeignKey(t => t.ProjectId).OnDelete(DeleteBehavior.Cascade);
        mb.Entity<AiSuggestionFeedback>().HasOne(t => t.User).WithMany().HasForeignKey(t => t.UserId).OnDelete(DeleteBehavior.Restrict);
        mb.Entity<ProjectTeamsLink>().HasOne(t => t.Project).WithOne(p => p.TeamsLink).HasForeignKey<ProjectTeamsLink>(t => t.ProjectId).OnDelete(DeleteBehavior.Cascade);
        mb.Entity<Project>().HasIndex(p => p.TenantId);
        mb.Entity<ProjectTask>().HasIndex(t => t.ProjectId);
        mb.Entity<ResourceAllocation>().HasIndex(r => new { r.UserId, r.ProjectId }).IsUnique();
        mb.Entity<ProjectTeamsLink>().HasIndex(t => t.ProjectId).IsUnique();

        var tenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var ownerId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var selinId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var emreId = Guid.Parse("55555555-5555-5555-5555-555555555555");
        var miraId = Guid.Parse("66666666-6666-6666-6666-666666666666");
        var canId = Guid.Parse("77777777-7777-7777-7777-777777777777");
        var lenaId = Guid.Parse("88888888-8888-8888-8888-888888888888");
        var jonasId = Guid.Parse("99999999-9999-9999-9999-999999999999");
        var philippId = Guid.Parse("10101010-1010-1010-1010-101010101010");

        var gShareId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var briefingId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var powerAppId = Guid.Parse("34343434-3434-3434-3434-343434343434");
        var posId = Guid.Parse("45454545-4545-4545-4545-454545454545");
        var governanceId = Guid.Parse("56565656-5656-5656-5656-565656565656");

        const string demoHash = "$2a$11$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2uheWG/igi.";

        mb.Entity<Tenant>().HasData(new Tenant { Id = tenantId, Name = "RealCore", Slug = "realcore" });
        mb.Entity<User>().HasData(
            new User { Id = ownerId, TenantId = tenantId, DisplayName = "Berk Can Atesoglu", Email = "berkcan@realcore.de", PasswordHash = demoHash, Role = "Projektleiter" },
            new User { Id = selinId, TenantId = tenantId, DisplayName = "Selin Kaya", Email = "selin@realcore.de", PasswordHash = demoHash, Role = "Product Owner" },
            new User { Id = emreId, TenantId = tenantId, DisplayName = "Emre Yilmaz", Email = "emre@realcore.de", PasswordHash = demoHash, Role = "Frontend Developer" },
            new User { Id = miraId, TenantId = tenantId, DisplayName = "Mira Hoffmann", Email = "mira@realcore.de", PasswordHash = demoHash, Role = "Backend Developer" },
            new User { Id = canId, TenantId = tenantId, DisplayName = "Can Demir", Email = "can@realcore.de", PasswordHash = demoHash, Role = "AI Engineer" },
            new User { Id = lenaId, TenantId = tenantId, DisplayName = "Lena Schmidt", Email = "lena@realcore.de", PasswordHash = demoHash, Role = "UX Designer" },
            new User { Id = jonasId, TenantId = tenantId, DisplayName = "Jonas Weber", Email = "jonas@realcore.de", PasswordHash = demoHash, Role = "QA Engineer" },
            new User { Id = philippId, TenantId = tenantId, DisplayName = "Philipp Schneider", Email = "philipp@realcore.de", PasswordHash = demoHash, Role = "Power Platform Developer" }
        );

        mb.Entity<Project>().HasData(
            new Project
            {
                Id = gShareId,
                TenantId = tenantId,
                OwnerId = ownerId,
                Name = "G-Share",
                Description = "Dokumentenablage und zentrale Plattform fuer Projektsteuerung, Teamtransparenz und operative Zusammenarbeit.",
                Customer = "RealCore Intern",
                Category = "product",
                Stage = "delivery",
                DeliveryModel = "Produktentwicklung",
                Sponsor = "Management",
                ExecutiveSummary = "G-Share ist das zentrale Arbeitsmittel fuer Delivery, Projektleitung und Dokumentenablage.",
                HealthSummary = "Scope ist stabil, Fokus liegt auf echter Projektsteuerung, Suchbarkeit und Governance-Faehigkeit.",
                Objective = "Ein zentrales Tool schaffen, in dem Projekte, Teams, Aufgaben, Entscheidungen und Dokumente an einem Ort gepflegt werden.",
                Scope = "Portfolio, Projekt-Detail, Ressourcen, Aufgabensteuerung, Notizen, Dokumente, Governance und Projektleiter-Workflows.",
                SuccessMetric = "Projektteams koennen alle relevanten Projektinformationen in unter 2 Minuten finden und pflegen.",
                Communication = "Wochenstatus montags, Team-Sync mittwochs, Review freitags.",
                NextMilestone = "Dokumentenablage und Governance-Cockpit live schalten",
                StakeholdersCsv = "Management|Projektleitung|Delivery Team",
                TechnologiesCsv = "Next.js|TypeScript|Zustand|Tailwind CSS|ASP.NET Core",
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
                Description = "Plugin fuer Teams und spaeter Copilot fuer schnelle Management-Briefings mit Projektstatus, Risiken und To-dos aus einer Quelle.",
                Customer = "RealCore Intern",
                Category = "product",
                Stage = "pilot",
                DeliveryModel = "Produktentwicklung",
                Sponsor = "Sales & Management",
                ExecutiveSummary = "Das Tool soll aus Live-Projektdaten belastbare Briefings fuer Management und Delivery erzeugen.",
                HealthSummary = "Funktionsfaehiger Kern vorhanden, der naechste Hebel ist die Einbettung in Teams und Copilot-Szenarien.",
                Objective = "Management-Briefings fuer Projekte automatisiert, kompakt und nachvollziehbar bereitstellen.",
                Scope = "KI-Assistenz, Projektabfragen, Priorisierung von Risiken und Management-Zusammenfassungen.",
                SuccessMetric = "Ein Briefing fuer ein Projekt kann in unter 30 Sekunden erstellt werden.",
                Communication = "Briefing Review dienstags, Prompt-Tuning donnerstags.",
                NextMilestone = "Teams-Plugin-Konzept finalisieren",
                StakeholdersCsv = "Management|Sales|Projektleitung",
                TechnologiesCsv = "Next.js|TypeScript|LLM Integration|Microsoft Teams",
                Status = "yellow",
                ProgressPercent = 48,
                BudgetTotal = 140000,
                BudgetSpent = 76000,
                StartDate = new DateOnly(2026, 2, 1),
                EndDate = new DateOnly(2026, 7, 15)
            },
            new Project
            {
                Id = powerAppId,
                TenantId = tenantId,
                OwnerId = ownerId,
                Name = "PowerApp Erweiterung - Freeform",
                Description = "Erweiterung der bestehenden PowerApp mit Freeform-Komponenten. Philipp ist in der Umsetzung.",
                Customer = "Interner Fachbereich",
                Category = "delivery",
                Stage = "implementation",
                DeliveryModel = "Power Platform Delivery",
                Sponsor = "Fachbereich Operations",
                ExecutiveSummary = "Ein bestaetigtes Delivery-Projekt mit klarer Umsetzungslinie ueber Philipp und enger Fachbereichsabstimmung.",
                HealthSummary = "Liegt auf Kurs, benoetigt aber saubere Entscheidungs- und Change-Request-Steuerung.",
                Objective = "Die bestehende PowerApp fachlich und technisch erweitern, ohne die bestehende Nutzung zu stoeren.",
                Scope = "Freeform-Komponenten, Fachbereichsfeedback, Change Requests, Test und Uebergabe.",
                SuccessMetric = "Der Fachbereich kann neue Freeform-Strecken ohne Medienbruch in der App nutzen.",
                Communication = "Jour fixe mit Fachbereich donnerstags, Umsetzungsreview montags.",
                NextMilestone = "Fachbereichs-Review fuer die erste Freeform-Strecke",
                StakeholdersCsv = "Philipp|Fachbereich|Projektleitung",
                TechnologiesCsv = "Power Apps|Power Automate|Dataverse",
                Status = "green",
                ProgressPercent = 41,
                BudgetTotal = 85000,
                BudgetSpent = 34000,
                StartDate = new DateOnly(2026, 3, 1),
                EndDate = new DateOnly(2026, 8, 15)
            },
            new Project
            {
                Id = posId,
                TenantId = tenantId,
                OwnerId = ownerId,
                Name = "Bestellsystem POS App",
                Description = "React-basierte POS-App. Standorte benoetigen Kassenpakete; Standortdaten kommen initial aus Excel.",
                Customer = "Retail Operations",
                Category = "rollout",
                Stage = "planning",
                DeliveryModel = "Custom Development",
                Sponsor = "Retail Operations",
                ExecutiveSummary = "Das Projekt verbindet Entwicklung, Rollout-Planung, Standortdaten und operative Kassenprozesse.",
                HealthSummary = "Anforderungs- und Datenlage sind noch volatil; Governance und Meilensteinsteuerung sind hier besonders wichtig.",
                Objective = "Ein skalierbares Bestellsystem fuer Standorte bereitstellen, das Kassenpakete und Standortdaten sauber verarbeitet.",
                Scope = "Excel-Import, Standortdaten, Paketlogik, POS-UI, Rolloutplanung und Test je Standort.",
                SuccessMetric = "Ein Standort kann in unter 10 Minuten angelegt und einem Kassenpaket zugeordnet werden.",
                Communication = "Rollout-Abstimmung dienstags, Technikreview freitags.",
                NextMilestone = "Excel-Import und Standortmodell abnehmen",
                StakeholdersCsv = "Retail Operations|Projektleitung|Entwicklung|Standorte",
                TechnologiesCsv = "React|TypeScript|Excel Import|REST API",
                Status = "yellow",
                ProgressPercent = 18,
                BudgetTotal = 220000,
                BudgetSpent = 28000,
                StartDate = new DateOnly(2026, 3, 15),
                EndDate = new DateOnly(2026, 10, 30)
            },
            new Project
            {
                Id = governanceId,
                TenantId = tenantId,
                OwnerId = ownerId,
                Name = "Governance",
                Description = "Querschnittsprojekt fuer PM-Standards, Stage Gates, Architektur, Risiko- und Entscheidungssteuerung.",
                Customer = "Management",
                Category = "governance",
                Stage = "setup",
                DeliveryModel = "PMO & Standards",
                Sponsor = "Management",
                ExecutiveSummary = "Governance ist der Hebel, um alle Projekte mit denselben Regeln, Gates und Berichtslinien zu steuern.",
                HealthSummary = "Muss parallel zu den Projekten aufgebaut werden, damit Skalierung und Steuerbarkeit funktionieren.",
                Objective = "Ein PMO-taugliches Governance-Modul mit Standards, Freigaben und klaren Verantwortlichkeiten aufbauen.",
                Scope = "Templates, Gate-Logik, Rollen, Standards, Review-Zyklen, Eskalationspfade.",
                SuccessMetric = "Alle Projekte folgen denselben Pflichtfeldern, Entscheidungswegen und Abnahme-Gates.",
                Communication = "Governance Review jeden Mittwoch mit Projektleitung und Management.",
                NextMilestone = "Stage-Gate-Checkliste fuer alle Projekte einführen",
                StakeholdersCsv = "Management|Projektleitung|Architektur|Delivery Leads",
                TechnologiesCsv = "PM Framework|Governance|Reporting",
                Status = "green",
                ProgressPercent = 27,
                BudgetTotal = 60000,
                BudgetSpent = 12000,
                StartDate = new DateOnly(2026, 3, 20),
                EndDate = new DateOnly(2026, 9, 30)
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
            new ResourceAllocation { Id = Guid.Parse("15151515-1515-1515-1515-151515151515"), UserId = lenaId, ProjectId = briefingId, AllocatedHours = 24, ProjectRole = "UX Designer", Responsibility = "Informationsarchitektur und Lesbarkeit" },
            new ResourceAllocation { Id = Guid.Parse("61616161-6161-6161-6161-616161616161"), UserId = ownerId, ProjectId = powerAppId, AllocatedHours = 12, ProjectRole = "Projektleiter", Responsibility = "Steuerung, Fachbereich, Abnahme" },
            new ResourceAllocation { Id = Guid.Parse("62626262-6262-6262-6262-626262626262"), UserId = philippId, ProjectId = powerAppId, AllocatedHours = 36, ProjectRole = "Lead Developer", Responsibility = "Umsetzung der Freeform-Erweiterung" },
            new ResourceAllocation { Id = Guid.Parse("63636363-6363-6363-6363-636363636363"), UserId = selinId, ProjectId = powerAppId, AllocatedHours = 10, ProjectRole = "Fachliche Steuerung", Responsibility = "Anforderungen und Priorisierung" },
            new ResourceAllocation { Id = Guid.Parse("64646464-6464-6464-6464-646464646464"), UserId = ownerId, ProjectId = posId, AllocatedHours = 20, ProjectRole = "Projektleiter", Responsibility = "Rollout-Steuerung, Stakeholder, Risiko-Management" },
            new ResourceAllocation { Id = Guid.Parse("65656565-6565-6565-6565-656565656565"), UserId = emreId, ProjectId = posId, AllocatedHours = 28, ProjectRole = "React Developer", Responsibility = "POS Frontend und Standort-UI" },
            new ResourceAllocation { Id = Guid.Parse("66666661-6666-6666-6666-666666666661"), UserId = miraId, ProjectId = posId, AllocatedHours = 24, ProjectRole = "Backend Developer", Responsibility = "Import, Standortdaten, Schnittstellen" },
            new ResourceAllocation { Id = Guid.Parse("67676767-6767-6767-6767-676767676767"), UserId = ownerId, ProjectId = governanceId, AllocatedHours = 14, ProjectRole = "Programmleitung", Responsibility = "Standards, Stage Gates, Reporting" },
            new ResourceAllocation { Id = Guid.Parse("68686868-6868-6868-6868-686868686868"), UserId = selinId, ProjectId = governanceId, AllocatedHours = 8, ProjectRole = "PMO", Responsibility = "Templates, Pflichtfelder, Review-Prozesse" },
            new ResourceAllocation { Id = Guid.Parse("69696969-6969-6969-6969-696969696969"), UserId = jonasId, ProjectId = governanceId, AllocatedHours = 6, ProjectRole = "Quality", Responsibility = "Abnahme- und Gate-Checklisten" }
        );

        mb.Entity<ProjectNote>().HasData(
            new ProjectNote { Id = Guid.Parse("16161616-1616-1616-1616-161616161616"), ProjectId = gShareId, AuthorId = ownerId, Title = "Kickoff Ergebnis", Content = "Projektstruktur, Rollen und erste Prioritaeten mit dem Team abgestimmt.", CreatedAt = new DateTime(2026, 2, 2, 9, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2026, 2, 2, 9, 0, 0, DateTimeKind.Utc) },
            new ProjectNote { Id = Guid.Parse("17171717-1717-1717-1717-171717171717"), ProjectId = gShareId, AuthorId = selinId, Title = "Nutzerfeedback", Content = "Projektleiter wollen Teammitglieder direkt am Projekt sehen und Notizen im Projekt pflegen.", CreatedAt = new DateTime(2026, 3, 18, 13, 30, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2026, 3, 18, 13, 30, 0, DateTimeKind.Utc) },
            new ProjectNote { Id = Guid.Parse("18181818-1818-1818-1818-181818181818"), ProjectId = briefingId, AuthorId = ownerId, Title = "Scope Fokus", Content = "Zunaechst nur zwei Projekte unterstuetzen, um das Briefing konsistent zu halten.", CreatedAt = new DateTime(2026, 3, 10, 10, 15, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2026, 3, 10, 10, 15, 0, DateTimeKind.Utc) },
            new ProjectNote { Id = Guid.Parse("19181818-1818-1818-1818-181818181819"), ProjectId = powerAppId, AuthorId = philippId, Title = "Umsetzungsstand", Content = "Philipp arbeitet an der ersten Freeform-Strecke, offene Punkte betreffen Datenvalidierung.", CreatedAt = new DateTime(2026, 3, 22, 9, 15, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2026, 3, 22, 9, 15, 0, DateTimeKind.Utc) },
            new ProjectNote { Id = Guid.Parse("20282828-2828-2828-2828-282828282828"), ProjectId = posId, AuthorId = ownerId, Title = "Projektansatz", Content = "Excel-Import ist initial gesetzt, spaeter ist eine Schnittstelle zur Stammdatenquelle geplant.", CreatedAt = new DateTime(2026, 3, 24, 7, 30, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2026, 3, 24, 7, 30, 0, DateTimeKind.Utc) },
            new ProjectNote { Id = Guid.Parse("30383838-3838-3838-3838-383838383838"), ProjectId = governanceId, AuthorId = ownerId, Title = "Governance Zielbild", Content = "Alle Delivery-Projekte sollen ueber dieselben Pflichtfelder, Gates und Freigabeschritte gefuehrt werden.", CreatedAt = new DateTime(2026, 3, 24, 8, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2026, 3, 24, 8, 0, 0, DateTimeKind.Utc) }
        );

        mb.Entity<ProjectLeadTask>().HasData(
            new ProjectLeadTask { Id = Guid.Parse("19191919-1919-1919-1919-191919191919"), ProjectId = gShareId, OwnerId = ownerId, Title = "Wochenstatus vorbereiten", Description = "Budget, Fortschritt und Risiken fuer den Wochenbericht aktualisieren.", DueDate = new DateOnly(2026, 3, 25), Status = "in_progress" },
            new ProjectLeadTask { Id = Guid.Parse("20202020-2020-2020-2020-202020202020"), ProjectId = gShareId, OwnerId = ownerId, Title = "Abnahme mit Stakeholdern planen", Description = "Termin fuer Review der Projekt-Detailseiten festlegen.", DueDate = new DateOnly(2026, 3, 29), Status = "todo" },
            new ProjectLeadTask { Id = Guid.Parse("21212121-2121-2121-2121-212121212121"), ProjectId = gShareId, OwnerId = ownerId, Title = "Offene Entscheidungen dokumentieren", Description = "Offene Scope-Fragen in den Projektnotizen festhalten.", DueDate = new DateOnly(2026, 3, 22), Status = "done" },
            new ProjectLeadTask { Id = Guid.Parse("22222221-2222-2222-2222-222222222221"), ProjectId = briefingId, OwnerId = ownerId, Title = "Prompt-Vorlagen abstimmen", Description = "Formulierung fuer Status- und Risikoantworten finalisieren.", DueDate = new DateOnly(2026, 3, 26), Status = "todo" },
            new ProjectLeadTask { Id = Guid.Parse("23232323-2323-2323-2323-232323232323"), ProjectId = briefingId, OwnerId = ownerId, Title = "Testbriefing mit Management teilen", Description = "Feedback fuer die erste Briefing-Version einholen.", DueDate = new DateOnly(2026, 3, 30), Status = "in_progress" },
            new ProjectLeadTask { Id = Guid.Parse("24242424-2424-2424-2424-242424242424"), ProjectId = powerAppId, OwnerId = ownerId, Title = "Review mit Philipp vorbereiten", Description = "Fachliche und technische Review-Punkte fuer die Freeform-Erweiterung vorbereiten.", DueDate = new DateOnly(2026, 3, 28), Status = "todo" },
            new ProjectLeadTask { Id = Guid.Parse("25252525-2525-2525-2525-252525252525"), ProjectId = posId, OwnerId = ownerId, Title = "Standortpakete definieren", Description = "Kassenpakete und Rollout-Zuordnung fachlich finalisieren.", DueDate = new DateOnly(2026, 4, 2), Status = "in_progress" },
            new ProjectLeadTask { Id = Guid.Parse("26262626-2626-2626-2626-262626262626"), ProjectId = governanceId, OwnerId = ownerId, Title = "Stage Gate Pflichtfelder festlegen", Description = "Pflichtfelder fuer Kickoff, Delivery und Go-Live definieren.", DueDate = new DateOnly(2026, 3, 31), Status = "todo" }
        );

        mb.Entity<ProjectMilestone>().HasData(
            new ProjectMilestone { Id = Guid.Parse("71717171-7171-7171-7171-717171717171"), ProjectId = gShareId, OwnerId = ownerId, Title = "Dokumentenablage Release", Description = "Projektakten, Ordnerstruktur und Dokumentenlisten in Produktion schalten.", DueDate = new DateOnly(2026, 4, 5), Status = "in_progress" },
            new ProjectMilestone { Id = Guid.Parse("72727272-7272-7272-7272-727272727272"), ProjectId = briefingId, OwnerId = canId, Title = "Teams Plugin Konzept", Description = "Einbindung in Microsoft Teams fachlich und technisch finalisieren.", DueDate = new DateOnly(2026, 4, 10), Status = "planned" },
            new ProjectMilestone { Id = Guid.Parse("73737373-7373-7373-7373-737373737373"), ProjectId = powerAppId, OwnerId = philippId, Title = "Freeform Review", Description = "Erste fachliche Abnahme der Freeform-Strecke.", DueDate = new DateOnly(2026, 4, 3), Status = "planned" },
            new ProjectMilestone { Id = Guid.Parse("74747474-7474-7474-7474-747474747474"), ProjectId = posId, OwnerId = ownerId, Title = "Excel Import Abnahme", Description = "Standortmodell und Importlogik gemeinsam mit Operations pruefen.", DueDate = new DateOnly(2026, 4, 12), Status = "planned" },
            new ProjectMilestone { Id = Guid.Parse("75757575-7575-7575-7575-757575757575"), ProjectId = governanceId, OwnerId = ownerId, Title = "Governance Gate v1", Description = "Pflichtcheckliste fuer neue Projekte ausrollen.", DueDate = new DateOnly(2026, 4, 1), Status = "in_progress" }
        );

        mb.Entity<ProjectDecision>().HasData(
            new ProjectDecision { Id = Guid.Parse("81818181-8181-8181-8181-818181818181"), ProjectId = gShareId, OwnerId = ownerId, Title = "Dokumentstruktur finalisieren", Context = "Es muss entschieden werden, ob Dokumente nach Projektphase oder Fachbereich organisiert werden.", Decision = "Projektphase bleibt der primaere Einstieg, Fachbereich wird als Filter abgebildet.", DueDate = new DateOnly(2026, 3, 29), Status = "done" },
            new ProjectDecision { Id = Guid.Parse("82828282-8282-8282-8282-828282828282"), ProjectId = briefingId, OwnerId = canId, Title = "Copilot Zielarchitektur", Context = "Klärung, ob zuerst Teams oder direkt Copilot priorisiert wird.", Decision = "Zuerst Teams Plugin, danach Copilot-Integration mit denselben Prompt-Bausteinen.", DueDate = new DateOnly(2026, 4, 4), Status = "open" },
            new ProjectDecision { Id = Guid.Parse("83838383-8383-8383-8383-838383838383"), ProjectId = powerAppId, OwnerId = philippId, Title = "Freeform Datenmodell", Context = "Neue Felder koennen in Dataverse oder lokal in der App gehalten werden.", Decision = "Noch offen, Dataverse-Pfad wird bewertet.", DueDate = new DateOnly(2026, 3, 30), Status = "open" },
            new ProjectDecision { Id = Guid.Parse("84848484-8484-8484-8484-848484848484"), ProjectId = posId, OwnerId = ownerId, Title = "Standortdatenquelle", Context = "Excel ist Startpunkt, langfristig wird eine Schnittstelle erwartet.", Decision = "Go-live mit Excel-Import, API-Schnittstelle als Folgephase.", DueDate = new DateOnly(2026, 4, 8), Status = "review" },
            new ProjectDecision { Id = Guid.Parse("85858585-8585-8585-8585-858585858585"), ProjectId = governanceId, OwnerId = ownerId, Title = "Pflicht-Gates fuer Delivery", Context = "Standardisierte Gates muessen fuer alle Projekte verbindlich werden.", Decision = "Kickoff, Scope Freeze, Test Readiness und Go-live sind Pflicht-Gates.", DueDate = new DateOnly(2026, 3, 31), Status = "done" }
        );

        mb.Entity<ProjectDocument>().HasData(
            new ProjectDocument { Id = Guid.Parse("91919191-9191-9191-9191-919191919191"), ProjectId = gShareId, OwnerId = ownerId, Title = "Projektsteckbrief", Category = "Projektakte", Url = "https://gshare.realcore.local/docs/g-share/steckbrief", Status = "approved" },
            new ProjectDocument { Id = Guid.Parse("92929292-9292-9292-9292-929292929292"), ProjectId = briefingId, OwnerId = canId, Title = "Teams Plugin Konzept", Category = "Konzept", Url = "https://gshare.realcore.local/docs/ai-briefing/teams-plugin", Status = "draft" },
            new ProjectDocument { Id = Guid.Parse("93939393-9393-9393-9393-939393939393"), ProjectId = powerAppId, OwnerId = philippId, Title = "Freeform Umsetzungsnotizen", Category = "Delivery", Url = "https://gshare.realcore.local/docs/freeform/umsetzung", Status = "draft" },
            new ProjectDocument { Id = Guid.Parse("94949494-9494-9494-9494-949494949494"), ProjectId = posId, OwnerId = ownerId, Title = "Excel Datenmapping", Category = "Spezifikation", Url = "https://gshare.realcore.local/docs/pos/excel-mapping", Status = "review" },
            new ProjectDocument { Id = Guid.Parse("95959595-9595-9595-9595-959595959595"), ProjectId = governanceId, OwnerId = ownerId, Title = "Governance Handbuch", Category = "Governance", Url = "https://gshare.realcore.local/docs/governance/handbuch", Status = "approved" }
        );

        mb.Entity<ProjectGovernanceCheck>().HasData(
            new ProjectGovernanceCheck { Id = Guid.Parse("a1a1a1a1-a1a1-a1a1-a1a1-a1a1a1a1a1a1"), ProjectId = gShareId, OwnerId = ownerId, Title = "Projektsteckbrief gepflegt", Area = "Kickoff", Notes = "Projektziele, Scope und Verantwortungen sind hinterlegt.", DueDate = new DateOnly(2026, 3, 27), Status = "done" },
            new ProjectGovernanceCheck { Id = Guid.Parse("a2a2a2a2-a2a2-a2a2-a2a2-a2a2a2a2a2a2"), ProjectId = briefingId, OwnerId = ownerId, Title = "Stakeholder-Review geplant", Area = "Steering", Notes = "Sales und Management muessen den Pilotumfang bestaetigen.", DueDate = new DateOnly(2026, 4, 3), Status = "open" },
            new ProjectGovernanceCheck { Id = Guid.Parse("a3a3a3a3-a3a3-a3a3-a3a3-a3a3a3a3a3a3"), ProjectId = powerAppId, OwnerId = ownerId, Title = "Change Request Prozess geklaert", Area = "Scope Control", Notes = "Fachbereichsaenderungen duerfen nur ueber Change Request aufgenommen werden.", DueDate = new DateOnly(2026, 4, 2), Status = "open" },
            new ProjectGovernanceCheck { Id = Guid.Parse("a4a4a4a4-a4a4-a4a4-a4a4-a4a4a4a4a4a4"), ProjectId = posId, OwnerId = ownerId, Title = "Rollout-Gate vorbereitet", Area = "Go-live", Notes = "Standortdaten, Paketzuordnung und Teststand muessen vor Rollout vollstaendig sein.", DueDate = new DateOnly(2026, 4, 15), Status = "open" },
            new ProjectGovernanceCheck { Id = Guid.Parse("a5a5a5a5-a5a5-a5a5-a5a5-a5a5a5a5a5a5"), ProjectId = governanceId, OwnerId = ownerId, Title = "Standard-Gates dokumentiert", Area = "PMO", Notes = "Pflicht-Gates fuer alle Projekte werden verbindlich dokumentiert.", DueDate = new DateOnly(2026, 3, 31), Status = "in_progress" }
        );

        mb.Entity<ProjectKnowledgeItem>().HasData(
            new ProjectKnowledgeItem { Id = Guid.Parse("b1b1b1b1-b1b1-b1b1-b1b1-b1b1b1b1b1b1"), ProjectId = gShareId, AuthorId = ownerId, Title = "Steering Protokoll Maerz", SourceType = "meeting", SourceLabel = "Steering 21.03.2026", Content = "Management erwartet eine belastbare Dokumentenstruktur, Suchfunktion und klare Verantwortlichkeiten fuer Projektakten. Entscheidung: Dokumente werden nach Projektphase gefuehrt und ueber Tags zusaetzlich nach Fachbereich filterbar gemacht.", TagsCsv = "steering|dokumente|governance", Importance = 5, CreatedAt = new DateTime(2026, 3, 21, 8, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2026, 3, 21, 8, 0, 0, DateTimeKind.Utc) },
            new ProjectKnowledgeItem { Id = Guid.Parse("b2b2b2b2-b2b2-b2b2-b2b2-b2b2b2b2b2b2"), ProjectId = briefingId, AuthorId = canId, Title = "Teams Discovery Notes", SourceType = "meeting", SourceLabel = "Workshop Teams Integration", Content = "Teams wird als erster Einstieg priorisiert. Nutzer wollen Management-Briefings direkt aus einem Projektkanal starten. Offener Punkt: Quellenbezug muss im Briefing sichtbar bleiben.", TagsCsv = "teams|copilot|briefing", Importance = 4, CreatedAt = new DateTime(2026, 3, 20, 10, 30, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2026, 3, 20, 10, 30, 0, DateTimeKind.Utc) },
            new ProjectKnowledgeItem { Id = Guid.Parse("b3b3b3b3-b3b3-b3b3-b3b3-b3b3b3b3b3b3"), ProjectId = powerAppId, AuthorId = philippId, Title = "Freeform Validierung", SourceType = "delivery_note", SourceLabel = "Philipp Update", Content = "Die Freeform-Strecke ist technisch umsetzbar. Kritisch ist noch die Validierung der Eingaben und die Frage, wie Fachbereichsaenderungen ohne Regression aufgenommen werden.", TagsCsv = "powerapp|validierung|change-request", Importance = 4, CreatedAt = new DateTime(2026, 3, 22, 9, 20, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2026, 3, 22, 9, 20, 0, DateTimeKind.Utc) },
            new ProjectKnowledgeItem { Id = Guid.Parse("b4b4b4b4-b4b4-b4b4-b4b4-b4b4b4b4b4b4"), ProjectId = posId, AuthorId = ownerId, Title = "Excel Import Erkenntnisse", SourceType = "import", SourceLabel = "Standorte_Maerz.xlsx", Content = "Die Standortliste ist unvollstaendig. Drei Standorte haben kein Kassenpaket, mehrere Zeilen enthalten doppelte IDs. Vor dem Rollout muss das Mapping mit Operations bereinigt werden.", TagsCsv = "excel|standorte|rollout", Importance = 5, CreatedAt = new DateTime(2026, 3, 24, 7, 45, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2026, 3, 24, 7, 45, 0, DateTimeKind.Utc) },
            new ProjectKnowledgeItem { Id = Guid.Parse("b5b5b5b5-b5b5-b5b5-b5b5-b5b5b5b5b5b5"), ProjectId = governanceId, AuthorId = ownerId, Title = "Governance Zielbild Workshop", SourceType = "meeting", SourceLabel = "PMO Workshop", Content = "Jedes Delivery-Projekt benoetigt ab sofort Pflicht-Gates, einen Entscheidungslog und einen nachweisbaren Abnahmestand. Das System soll Luecken automatisch markieren und Vorschlaege generieren.", TagsCsv = "pmo|stage-gate|standards", Importance = 5, CreatedAt = new DateTime(2026, 3, 24, 8, 15, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2026, 3, 24, 8, 15, 0, DateTimeKind.Utc) }
        );

        mb.Entity<AiSuggestionFeedback>().HasData(
            new AiSuggestionFeedback { Id = Guid.Parse("c1c1c1c1-c1c1-c1c1-c1c1-c1c1c1c1c1c1"), ProjectId = gShareId, UserId = ownerId, SuggestionType = "governance", SuggestionTitle = "Governance-Luecke schliessen", Status = "accepted", Notes = "Wird im kommenden Steering direkt aufgenommen.", CreatedAt = new DateTime(2026, 3, 24, 8, 30, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2026, 3, 24, 8, 30, 0, DateTimeKind.Utc) }
        );

        mb.Entity<ProjectTeamsLink>().HasData(
            new ProjectTeamsLink { Id = Guid.Parse("d1d1d1d1-d1d1-d1d1-d1d1-d1d1d1d1d1d1"), ProjectId = briefingId, TeamName = "AI Briefing Tool", ChannelName = "briefing-pilot", TeamId = "teams-ai-briefing", ChannelId = "channel-briefing-pilot", TenantDomain = "realcore.onmicrosoft.com", SyncStatus = "planned" },
            new ProjectTeamsLink { Id = Guid.Parse("d2d2d2d2-d2d2-d2d2-d2d2-d2d2d2d2d2d2"), ProjectId = posId, TeamName = "POS Rollout", ChannelName = "weekly-sync", TeamId = "teams-pos-rollout", ChannelId = "channel-weekly-sync", TenantDomain = "realcore.onmicrosoft.com", SyncStatus = "planned" }
        );
    }
}
