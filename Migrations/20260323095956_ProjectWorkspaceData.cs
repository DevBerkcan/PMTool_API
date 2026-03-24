using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PmTool.Api.Migrations
{
    /// <inheritdoc />
    public partial class ProjectWorkspaceData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "ResourceAllocations",
                keyColumn: "Id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));

            migrationBuilder.AddColumn<string>(
                name: "ProjectRole",
                table: "ResourceAllocations",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Responsibility",
                table: "ResourceAllocations",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Communication",
                table: "Projects",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NextMilestone",
                table: "Projects",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Objective",
                table: "Projects",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Scope",
                table: "Projects",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "StakeholdersCsv",
                table: "Projects",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SuccessMetric",
                table: "Projects",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TechnologiesCsv",
                table: "Projects",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "ProjectLeadTasks",
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
                    table.PrimaryKey("PK_ProjectLeadTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectLeadTasks_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectLeadTasks_Users_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProjectNotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProjectId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AuthorId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Content = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectNotes_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectNotes_Users_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "ProjectLeadTasks",
                columns: new[] { "Id", "CreatedAt", "Description", "DueDate", "OwnerId", "ProjectId", "Status", "Title", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("19191919-1919-1919-1919-191919191919"), new DateTime(2026, 3, 23, 9, 59, 54, 364, DateTimeKind.Utc).AddTicks(6860), "Budget, Fortschritt und Risiken fuer den Wochenbericht aktualisieren.", new DateOnly(2026, 3, 25), new Guid("22222222-2222-2222-2222-222222222222"), new Guid("33333333-3333-3333-3333-333333333333"), "in_progress", "Wochenstatus vorbereiten", new DateTime(2026, 3, 23, 9, 59, 54, 364, DateTimeKind.Utc).AddTicks(6861) },
                    { new Guid("20202020-2020-2020-2020-202020202020"), new DateTime(2026, 3, 23, 9, 59, 54, 364, DateTimeKind.Utc).AddTicks(6864), "Termin fuer Review der Projekt-Detailseiten festlegen.", new DateOnly(2026, 3, 29), new Guid("22222222-2222-2222-2222-222222222222"), new Guid("33333333-3333-3333-3333-333333333333"), "todo", "Abnahme mit Stakeholdern planen", new DateTime(2026, 3, 23, 9, 59, 54, 364, DateTimeKind.Utc).AddTicks(6865) },
                    { new Guid("21212121-2121-2121-2121-212121212121"), new DateTime(2026, 3, 23, 9, 59, 54, 364, DateTimeKind.Utc).AddTicks(6867), "Offene Scope-Fragen in den Projektnotizen festhalten.", new DateOnly(2026, 3, 22), new Guid("22222222-2222-2222-2222-222222222222"), new Guid("33333333-3333-3333-3333-333333333333"), "done", "Offene Entscheidungen dokumentieren", new DateTime(2026, 3, 23, 9, 59, 54, 364, DateTimeKind.Utc).AddTicks(6868) }
                });

            migrationBuilder.InsertData(
                table: "ProjectNotes",
                columns: new[] { "Id", "AuthorId", "Content", "CreatedAt", "ProjectId", "Title", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("16161616-1616-1616-1616-161616161616"), new Guid("22222222-2222-2222-2222-222222222222"), "Projektstruktur, Rollen und erste Prioritaeten mit dem Team abgestimmt.", new DateTime(2026, 2, 2, 9, 0, 0, 0, DateTimeKind.Utc), new Guid("33333333-3333-3333-3333-333333333333"), "Kickoff Ergebnis", new DateTime(2026, 2, 2, 9, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("17171717-1717-1717-1717-171717171717"), new Guid("44444444-4444-4444-4444-444444444444"), "Projektleiter wollen Teammitglieder direkt am Projekt sehen und Notizen im Projekt pflegen.", new DateTime(2026, 3, 18, 13, 30, 0, 0, DateTimeKind.Utc), new Guid("33333333-3333-3333-3333-333333333333"), "Nutzerfeedback", new DateTime(2026, 3, 18, 13, 30, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.UpdateData(
                table: "Projects",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                columns: new[] { "BudgetSpent", "BudgetTotal", "Communication", "CreatedAt", "Customer", "Description", "EndDate", "Name", "NextMilestone", "Objective", "ProgressPercent", "Scope", "StakeholdersCsv", "StartDate", "SuccessMetric", "TechnologiesCsv", "UpdatedAt" },
                values: new object[] { 126000m, 180000m, "Wochenstatus montags, Team-Sync mittwochs, Review freitags.", new DateTime(2026, 3, 23, 9, 59, 54, 364, DateTimeKind.Utc).AddTicks(6718), "RealCore Intern", "Zentrale Plattform fuer Projektsteuerung, Teamtransparenz und operative Zusammenarbeit.", new DateOnly(2026, 6, 30), "G-Share", "Projekt-Detailseite mit Team und Notizen live schalten", "Ein zentrales Tool schaffen, in dem Projekte, Teams, Aufgaben und Notizen an einem Ort gepflegt werden.", 72, "Portfolio, Projekt-Detail, Ressourcen, Aufgabensteuerung, Notizen und Projektleiter-Workflows.", "Management|Projektleitung|Delivery Team", new DateOnly(2026, 1, 15), "Projektteams koennen alle relevanten Projektinformationen in unter 2 Minuten finden und pflegen.", "Next.js|TypeScript|Zustand|Tailwind CSS", new DateTime(2026, 3, 23, 9, 59, 54, 364, DateTimeKind.Utc).AddTicks(6719) });

            migrationBuilder.InsertData(
                table: "Projects",
                columns: new[] { "Id", "BudgetSpent", "BudgetTotal", "Communication", "CreatedAt", "Customer", "Description", "EndDate", "Name", "NextMilestone", "Objective", "OwnerId", "ProgressPercent", "Scope", "StakeholdersCsv", "StartDate", "Status", "SuccessMetric", "TechnologiesCsv", "TenantId", "UpdatedAt" },
                values: new object[] { new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), 76000m, 140000m, "Briefing Review dienstags, Prompt-Tuning donnerstags.", new DateTime(2026, 3, 23, 9, 59, 54, 364, DateTimeKind.Utc).AddTicks(6737), "RealCore Intern", "Tool fuer schnelle Management-Briefings mit Projektstatus, Risiken und To-dos aus einer Quelle.", new DateOnly(2026, 7, 15), "AI Briefing Tool", "Status- und Risikoantworten auf die neuen Projekte umstellen", "Management-Briefings fuer Projekte automatisiert, kompakt und nachvollziehbar bereitstellen.", new Guid("22222222-2222-2222-2222-222222222222"), 48, "KI-Assistenz, Projektabfragen, Priorisierung von Risiken und Management-Zusammenfassungen.", "Management|Sales|Projektleitung", new DateOnly(2026, 2, 1), "yellow", "Ein Briefing fuer ein Projekt kann in unter 30 Sekunden erstellt werden.", "Next.js|TypeScript|LLM Integration|Framer Motion", new Guid("11111111-1111-1111-1111-111111111111"), new DateTime(2026, 3, 23, 9, 59, 54, 364, DateTimeKind.Utc).AddTicks(6737) });

            migrationBuilder.UpdateData(
                table: "ResourceAllocations",
                keyColumn: "Id",
                keyValue: new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                columns: new[] { "AllocatedHours", "CreatedAt", "ProjectRole", "Responsibility", "UpdatedAt", "UserId" },
                values: new object[] { 34, new DateTime(2026, 3, 23, 9, 59, 54, 364, DateTimeKind.Utc).AddTicks(6768), "Projektleiter", "Steuerung, Stakeholder-Management, Priorisierung", new DateTime(2026, 3, 23, 9, 59, 54, 364, DateTimeKind.Utc).AddTicks(6768), new Guid("22222222-2222-2222-2222-222222222222") });

            migrationBuilder.UpdateData(
                table: "ResourceAllocations",
                keyColumn: "Id",
                keyValue: new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc"),
                columns: new[] { "AllocatedHours", "CreatedAt", "ProjectRole", "Responsibility", "UpdatedAt", "UserId" },
                values: new object[] { 30, new DateTime(2026, 3, 23, 9, 59, 54, 364, DateTimeKind.Utc).AddTicks(6775), "Product Owner", "Anforderungen, Backlog, Fachseite", new DateTime(2026, 3, 23, 9, 59, 54, 364, DateTimeKind.Utc).AddTicks(6775), new Guid("44444444-4444-4444-4444-444444444444") });

            migrationBuilder.UpdateData(
                table: "ResourceAllocations",
                keyColumn: "Id",
                keyValue: new Guid("dddddddd-dddd-dddd-dddd-dddddddddddd"),
                columns: new[] { "AllocatedHours", "CreatedAt", "ProjectRole", "Responsibility", "UpdatedAt", "UserId" },
                values: new object[] { 36, new DateTime(2026, 3, 23, 9, 59, 54, 364, DateTimeKind.Utc).AddTicks(6777), "Frontend Developer", "Dashboard, Projektseiten, UX-Umsetzung", new DateTime(2026, 3, 23, 9, 59, 54, 364, DateTimeKind.Utc).AddTicks(6778), new Guid("55555555-5555-5555-5555-555555555555") });

            migrationBuilder.UpdateData(
                table: "ResourceAllocations",
                keyColumn: "Id",
                keyValue: new Guid("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"),
                columns: new[] { "AllocatedHours", "CreatedAt", "ProjectRole", "Responsibility", "UpdatedAt", "UserId" },
                values: new object[] { 32, new DateTime(2026, 3, 23, 9, 59, 54, 364, DateTimeKind.Utc).AddTicks(6780), "Backend Developer", "APIs, Datenmodell, Berechtigungen", new DateTime(2026, 3, 23, 9, 59, 54, 364, DateTimeKind.Utc).AddTicks(6780), new Guid("66666666-6666-6666-6666-666666666666") });

            migrationBuilder.UpdateData(
                table: "ResourceAllocations",
                keyColumn: "Id",
                keyValue: new Guid("ffffffff-ffff-ffff-ffff-ffffffffffff"),
                columns: new[] { "CreatedAt", "ProjectRole", "Responsibility", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 23, 9, 59, 54, 364, DateTimeKind.Utc).AddTicks(6783), "QA Engineer", "Testfaelle, Regression, Abnahme", new DateTime(2026, 3, 23, 9, 59, 54, 364, DateTimeKind.Utc).AddTicks(6783) });

            migrationBuilder.UpdateData(
                table: "Tenants",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "CreatedAt", "Name", "Slug", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 23, 9, 59, 54, 364, DateTimeKind.Utc).AddTicks(6468), "RealCore", "realcore", new DateTime(2026, 3, 23, 9, 59, 54, 364, DateTimeKind.Utc).AddTicks(6472) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "CreatedAt", "DisplayName", "Role", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 23, 9, 59, 54, 364, DateTimeKind.Utc).AddTicks(6600), "Berk Can Atesoglu", "Projektleiter", new DateTime(2026, 3, 23, 9, 59, 54, 364, DateTimeKind.Utc).AddTicks(6601) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                columns: new[] { "CreatedAt", "DisplayName", "Email", "Role", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 23, 9, 59, 54, 364, DateTimeKind.Utc).AddTicks(6614), "Selin Kaya", "selin@realcore.de", "Product Owner", new DateTime(2026, 3, 23, 9, 59, 54, 364, DateTimeKind.Utc).AddTicks(6614) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"),
                columns: new[] { "CreatedAt", "DisplayName", "Email", "Role", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 23, 9, 59, 54, 364, DateTimeKind.Utc).AddTicks(6616), "Emre Yilmaz", "emre@realcore.de", "Frontend Developer", new DateTime(2026, 3, 23, 9, 59, 54, 364, DateTimeKind.Utc).AddTicks(6617) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("66666666-6666-6666-6666-666666666666"),
                columns: new[] { "CreatedAt", "DisplayName", "Email", "Role", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 23, 9, 59, 54, 364, DateTimeKind.Utc).AddTicks(6619), "Mira Hoffmann", "mira@realcore.de", "Backend Developer", new DateTime(2026, 3, 23, 9, 59, 54, 364, DateTimeKind.Utc).AddTicks(6619) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("77777777-7777-7777-7777-777777777777"),
                columns: new[] { "CreatedAt", "DisplayName", "Email", "Role", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 23, 9, 59, 54, 364, DateTimeKind.Utc).AddTicks(6621), "Can Demir", "can@realcore.de", "AI Engineer", new DateTime(2026, 3, 23, 9, 59, 54, 364, DateTimeKind.Utc).AddTicks(6621) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("88888888-8888-8888-8888-888888888888"),
                columns: new[] { "CreatedAt", "DisplayName", "Email", "Role", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 23, 9, 59, 54, 364, DateTimeKind.Utc).AddTicks(6623), "Lena Schmidt", "lena@realcore.de", "UX Designer", new DateTime(2026, 3, 23, 9, 59, 54, 364, DateTimeKind.Utc).AddTicks(6624) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("99999999-9999-9999-9999-999999999999"),
                columns: new[] { "CreatedAt", "DisplayName", "Email", "Role", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 23, 9, 59, 54, 364, DateTimeKind.Utc).AddTicks(6626), "Jonas Weber", "jonas@realcore.de", "QA Engineer", new DateTime(2026, 3, 23, 9, 59, 54, 364, DateTimeKind.Utc).AddTicks(6626) });

            migrationBuilder.InsertData(
                table: "ProjectLeadTasks",
                columns: new[] { "Id", "CreatedAt", "Description", "DueDate", "OwnerId", "ProjectId", "Status", "Title", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("22222221-2222-2222-2222-222222222221"), new DateTime(2026, 3, 23, 9, 59, 54, 364, DateTimeKind.Utc).AddTicks(6871), "Formulierung fuer Status- und Risikoantworten finalisieren.", new DateOnly(2026, 3, 26), new Guid("22222222-2222-2222-2222-222222222222"), new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), "todo", "Prompt-Vorlagen abstimmen", new DateTime(2026, 3, 23, 9, 59, 54, 364, DateTimeKind.Utc).AddTicks(6871) },
                    { new Guid("23232323-2323-2323-2323-232323232323"), new DateTime(2026, 3, 23, 9, 59, 54, 364, DateTimeKind.Utc).AddTicks(6877), "Feedback fuer die erste Briefing-Version einholen.", new DateOnly(2026, 3, 30), new Guid("22222222-2222-2222-2222-222222222222"), new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), "in_progress", "Testbriefing mit Management teilen", new DateTime(2026, 3, 23, 9, 59, 54, 364, DateTimeKind.Utc).AddTicks(6877) }
                });

            migrationBuilder.InsertData(
                table: "ProjectNotes",
                columns: new[] { "Id", "AuthorId", "Content", "CreatedAt", "ProjectId", "Title", "UpdatedAt" },
                values: new object[] { new Guid("18181818-1818-1818-1818-181818181818"), new Guid("22222222-2222-2222-2222-222222222222"), "Zunaechst nur zwei Projekte unterstuetzen, um das Briefing konsistent zu halten.", new DateTime(2026, 3, 10, 10, 15, 0, 0, DateTimeKind.Utc), new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), "Scope Fokus", new DateTime(2026, 3, 10, 10, 15, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.InsertData(
                table: "ResourceAllocations",
                columns: new[] { "Id", "AllocatedHours", "CreatedAt", "ProjectId", "ProjectRole", "Responsibility", "UpdatedAt", "UserId" },
                values: new object[,]
                {
                    { new Guid("12121212-1212-1212-1212-121212121212"), 18, new DateTime(2026, 3, 23, 9, 59, 54, 364, DateTimeKind.Utc).AddTicks(6785), new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), "Projektleiter", "Roadmap, Stakeholder, Freigaben", new DateTime(2026, 3, 23, 9, 59, 54, 364, DateTimeKind.Utc).AddTicks(6786), new Guid("22222222-2222-2222-2222-222222222222") },
                    { new Guid("13131313-1313-1313-1313-131313131313"), 38, new DateTime(2026, 3, 23, 9, 59, 54, 364, DateTimeKind.Utc).AddTicks(6789), new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), "AI Engineer", "Prompting, Auswertung, Response-Logik", new DateTime(2026, 3, 23, 9, 59, 54, 364, DateTimeKind.Utc).AddTicks(6789), new Guid("77777777-7777-7777-7777-777777777777") },
                    { new Guid("14141414-1414-1414-1414-141414141414"), 20, new DateTime(2026, 3, 23, 9, 59, 54, 364, DateTimeKind.Utc).AddTicks(6791), new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), "Frontend Developer", "Chat-UI und Briefing-Darstellung", new DateTime(2026, 3, 23, 9, 59, 54, 364, DateTimeKind.Utc).AddTicks(6792), new Guid("55555555-5555-5555-5555-555555555555") },
                    { new Guid("15151515-1515-1515-1515-151515151515"), 24, new DateTime(2026, 3, 23, 9, 59, 54, 364, DateTimeKind.Utc).AddTicks(6796), new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), "UX Designer", "Informationsarchitektur und Lesbarkeit", new DateTime(2026, 3, 23, 9, 59, 54, 364, DateTimeKind.Utc).AddTicks(6797), new Guid("88888888-8888-8888-8888-888888888888") }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectLeadTasks_OwnerId",
                table: "ProjectLeadTasks",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectLeadTasks_ProjectId",
                table: "ProjectLeadTasks",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectNotes_AuthorId",
                table: "ProjectNotes",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectNotes_ProjectId",
                table: "ProjectNotes",
                column: "ProjectId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProjectLeadTasks");

            migrationBuilder.DropTable(
                name: "ProjectNotes");

            migrationBuilder.DeleteData(
                table: "ResourceAllocations",
                keyColumn: "Id",
                keyValue: new Guid("12121212-1212-1212-1212-121212121212"));

            migrationBuilder.DeleteData(
                table: "ResourceAllocations",
                keyColumn: "Id",
                keyValue: new Guid("13131313-1313-1313-1313-131313131313"));

            migrationBuilder.DeleteData(
                table: "ResourceAllocations",
                keyColumn: "Id",
                keyValue: new Guid("14141414-1414-1414-1414-141414141414"));

            migrationBuilder.DeleteData(
                table: "ResourceAllocations",
                keyColumn: "Id",
                keyValue: new Guid("15151515-1515-1515-1515-151515151515"));

            migrationBuilder.DeleteData(
                table: "Projects",
                keyColumn: "Id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));

            migrationBuilder.DropColumn(
                name: "ProjectRole",
                table: "ResourceAllocations");

            migrationBuilder.DropColumn(
                name: "Responsibility",
                table: "ResourceAllocations");

            migrationBuilder.DropColumn(
                name: "Communication",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "NextMilestone",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "Objective",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "Scope",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "StakeholdersCsv",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "SuccessMetric",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "TechnologiesCsv",
                table: "Projects");

            migrationBuilder.UpdateData(
                table: "Projects",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                columns: new[] { "BudgetSpent", "BudgetTotal", "CreatedAt", "Customer", "Description", "EndDate", "Name", "ProgressPercent", "StartDate", "UpdatedAt" },
                values: new object[] { 520000m, 850000m, new DateTime(2026, 3, 19, 14, 38, 44, 501, DateTimeKind.Utc).AddTicks(9864), "Gebr. Heinemann", "Implementierung SAP S/4HANA", new DateOnly(2026, 9, 30), "SAP Retail Rollout 2026", 68, new DateOnly(2026, 1, 1), new DateTime(2026, 3, 19, 14, 38, 44, 501, DateTimeKind.Utc).AddTicks(9864) });

            migrationBuilder.UpdateData(
                table: "ResourceAllocations",
                keyColumn: "Id",
                keyValue: new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                columns: new[] { "AllocatedHours", "CreatedAt", "UpdatedAt", "UserId" },
                values: new object[] { 32, new DateTime(2026, 3, 19, 14, 38, 44, 501, DateTimeKind.Utc).AddTicks(9900), new DateTime(2026, 3, 19, 14, 38, 44, 501, DateTimeKind.Utc).AddTicks(9901), new Guid("55555555-5555-5555-5555-555555555555") });

            migrationBuilder.UpdateData(
                table: "ResourceAllocations",
                keyColumn: "Id",
                keyValue: new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc"),
                columns: new[] { "AllocatedHours", "CreatedAt", "UpdatedAt", "UserId" },
                values: new object[] { 20, new DateTime(2026, 3, 19, 14, 38, 44, 501, DateTimeKind.Utc).AddTicks(9902), new DateTime(2026, 3, 19, 14, 38, 44, 501, DateTimeKind.Utc).AddTicks(9903), new Guid("66666666-6666-6666-6666-666666666666") });

            migrationBuilder.UpdateData(
                table: "ResourceAllocations",
                keyColumn: "Id",
                keyValue: new Guid("dddddddd-dddd-dddd-dddd-dddddddddddd"),
                columns: new[] { "AllocatedHours", "CreatedAt", "UpdatedAt", "UserId" },
                values: new object[] { 16, new DateTime(2026, 3, 19, 14, 38, 44, 501, DateTimeKind.Utc).AddTicks(9904), new DateTime(2026, 3, 19, 14, 38, 44, 501, DateTimeKind.Utc).AddTicks(9904), new Guid("77777777-7777-7777-7777-777777777777") });

            migrationBuilder.UpdateData(
                table: "ResourceAllocations",
                keyColumn: "Id",
                keyValue: new Guid("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"),
                columns: new[] { "AllocatedHours", "CreatedAt", "UpdatedAt", "UserId" },
                values: new object[] { 45, new DateTime(2026, 3, 19, 14, 38, 44, 501, DateTimeKind.Utc).AddTicks(9906), new DateTime(2026, 3, 19, 14, 38, 44, 501, DateTimeKind.Utc).AddTicks(9906), new Guid("88888888-8888-8888-8888-888888888888") });

            migrationBuilder.UpdateData(
                table: "ResourceAllocations",
                keyColumn: "Id",
                keyValue: new Guid("ffffffff-ffff-ffff-ffff-ffffffffffff"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 19, 14, 38, 44, 501, DateTimeKind.Utc).AddTicks(9908), new DateTime(2026, 3, 19, 14, 38, 44, 501, DateTimeKind.Utc).AddTicks(9908) });

            migrationBuilder.InsertData(
                table: "ResourceAllocations",
                columns: new[] { "Id", "AllocatedHours", "CreatedAt", "ProjectId", "UpdatedAt", "UserId" },
                values: new object[] { new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), 38, new DateTime(2026, 3, 19, 14, 38, 44, 501, DateTimeKind.Utc).AddTicks(9896), new Guid("33333333-3333-3333-3333-333333333333"), new DateTime(2026, 3, 19, 14, 38, 44, 501, DateTimeKind.Utc).AddTicks(9896), new Guid("44444444-4444-4444-4444-444444444444") });

            migrationBuilder.UpdateData(
                table: "Tenants",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "CreatedAt", "Name", "Slug", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 19, 14, 38, 44, 501, DateTimeKind.Utc).AddTicks(9732), "Gebr. Heinemann", "heinemann", new DateTime(2026, 3, 19, 14, 38, 44, 501, DateTimeKind.Utc).AddTicks(9736) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "CreatedAt", "DisplayName", "Role", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 19, 14, 38, 44, 501, DateTimeKind.Utc).AddTicks(9830), "Berk-Can Atesoglu", "Admin", new DateTime(2026, 3, 19, 14, 38, 44, 501, DateTimeKind.Utc).AddTicks(9831) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                columns: new[] { "CreatedAt", "DisplayName", "Email", "Role", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 19, 14, 38, 44, 501, DateTimeKind.Utc).AddTicks(9834), "Anna Schmidt", "anna@heinemann.de", "SAP Consultant", new DateTime(2026, 3, 19, 14, 38, 44, 501, DateTimeKind.Utc).AddTicks(9834) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"),
                columns: new[] { "CreatedAt", "DisplayName", "Email", "Role", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 19, 14, 38, 44, 501, DateTimeKind.Utc).AddTicks(9836), "Klaus Weber", "klaus@heinemann.de", "Data Engineer", new DateTime(2026, 3, 19, 14, 38, 44, 501, DateTimeKind.Utc).AddTicks(9836) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("66666666-6666-6666-6666-666666666666"),
                columns: new[] { "CreatedAt", "DisplayName", "Email", "Role", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 19, 14, 38, 44, 501, DateTimeKind.Utc).AddTicks(9838), "Lisa Braun", "lisa@heinemann.de", "QA Engineer", new DateTime(2026, 3, 19, 14, 38, 44, 501, DateTimeKind.Utc).AddTicks(9838) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("77777777-7777-7777-7777-777777777777"),
                columns: new[] { "CreatedAt", "DisplayName", "Email", "Role", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 19, 14, 38, 44, 501, DateTimeKind.Utc).AddTicks(9840), "Tom Fischer", "tom@heinemann.de", "Trainer", new DateTime(2026, 3, 19, 14, 38, 44, 501, DateTimeKind.Utc).AddTicks(9840) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("88888888-8888-8888-8888-888888888888"),
                columns: new[] { "CreatedAt", "DisplayName", "Email", "Role", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 19, 14, 38, 44, 501, DateTimeKind.Utc).AddTicks(9842), "Maria Hoffmann", "maria@heinemann.de", "Dev Lead", new DateTime(2026, 3, 19, 14, 38, 44, 501, DateTimeKind.Utc).AddTicks(9842) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("99999999-9999-9999-9999-999999999999"),
                columns: new[] { "CreatedAt", "DisplayName", "Email", "Role", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 19, 14, 38, 44, 501, DateTimeKind.Utc).AddTicks(9843), "Peter Jung", "peter@heinemann.de", "Business Analyst", new DateTime(2026, 3, 19, 14, 38, 44, 501, DateTimeKind.Utc).AddTicks(9844) });
        }
    }
}
