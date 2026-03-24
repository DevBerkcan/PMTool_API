using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PmTool.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddResourceAllocationsAndSeedTeam : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS "ResourceAllocations" (
                    "Id" TEXT NOT NULL CONSTRAINT "PK_ResourceAllocations" PRIMARY KEY,
                    "UserId" TEXT NOT NULL,
                    "ProjectId" TEXT NOT NULL,
                    "AllocatedHours" INTEGER NOT NULL,
                    "CreatedAt" TEXT NOT NULL,
                    "UpdatedAt" TEXT NOT NULL,
                    CONSTRAINT "FK_ResourceAllocations_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE,
                    CONSTRAINT "FK_ResourceAllocations_Projects_ProjectId" FOREIGN KEY ("ProjectId") REFERENCES "Projects" ("Id") ON DELETE CASCADE
                );
                """);

            migrationBuilder.UpdateData(
                table: "Projects",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 19, 14, 38, 44, 501, DateTimeKind.Utc).AddTicks(9864), new DateTime(2026, 3, 19, 14, 38, 44, 501, DateTimeKind.Utc).AddTicks(9864) });

            migrationBuilder.UpdateData(
                table: "Tenants",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 19, 14, 38, 44, 501, DateTimeKind.Utc).AddTicks(9732), new DateTime(2026, 3, 19, 14, 38, 44, 501, DateTimeKind.Utc).AddTicks(9736) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "CreatedAt", "DisplayName", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 19, 14, 38, 44, 501, DateTimeKind.Utc).AddTicks(9830), "Berk-Can Atesoglu", new DateTime(2026, 3, 19, 14, 38, 44, 501, DateTimeKind.Utc).AddTicks(9831) });

            migrationBuilder.Sql("""
                INSERT OR IGNORE INTO "Users" ("Id", "CreatedAt", "DisplayName", "Email", "PasswordHash", "Role", "TenantId", "UpdatedAt") VALUES
                ('44444444-4444-4444-4444-444444444444', '2026-03-19T14:38:44.5019834Z', 'Anna Schmidt', 'anna@heinemann.de', '$2a$11$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2uheWG/igi.', 'SAP Consultant', '11111111-1111-1111-1111-111111111111', '2026-03-19T14:38:44.5019834Z'),
                ('55555555-5555-5555-5555-555555555555', '2026-03-19T14:38:44.5019836Z', 'Klaus Weber', 'klaus@heinemann.de', '$2a$11$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2uheWG/igi.', 'Data Engineer', '11111111-1111-1111-1111-111111111111', '2026-03-19T14:38:44.5019836Z'),
                ('66666666-6666-6666-6666-666666666666', '2026-03-19T14:38:44.5019838Z', 'Lisa Braun', 'lisa@heinemann.de', '$2a$11$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2uheWG/igi.', 'QA Engineer', '11111111-1111-1111-1111-111111111111', '2026-03-19T14:38:44.5019838Z'),
                ('77777777-7777-7777-7777-777777777777', '2026-03-19T14:38:44.5019840Z', 'Tom Fischer', 'tom@heinemann.de', '$2a$11$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2uheWG/igi.', 'Trainer', '11111111-1111-1111-1111-111111111111', '2026-03-19T14:38:44.5019840Z'),
                ('88888888-8888-8888-8888-888888888888', '2026-03-19T14:38:44.5019842Z', 'Maria Hoffmann', 'maria@heinemann.de', '$2a$11$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2uheWG/igi.', 'Dev Lead', '11111111-1111-1111-1111-111111111111', '2026-03-19T14:38:44.5019842Z'),
                ('99999999-9999-9999-9999-999999999999', '2026-03-19T14:38:44.5019844Z', 'Peter Jung', 'peter@heinemann.de', '$2a$11$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2uheWG/igi.', 'Business Analyst', '11111111-1111-1111-1111-111111111111', '2026-03-19T14:38:44.5019844Z');
                """);

            migrationBuilder.Sql("""
                INSERT OR IGNORE INTO "ResourceAllocations" ("Id", "AllocatedHours", "CreatedAt", "ProjectId", "UpdatedAt", "UserId") VALUES
                ('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 38, '2026-03-19T14:38:44.5019896Z', '33333333-3333-3333-3333-333333333333', '2026-03-19T14:38:44.5019896Z', '44444444-4444-4444-4444-444444444444'),
                ('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', 32, '2026-03-19T14:38:44.5019900Z', '33333333-3333-3333-3333-333333333333', '2026-03-19T14:38:44.5019901Z', '55555555-5555-5555-5555-555555555555'),
                ('cccccccc-cccc-cccc-cccc-cccccccccccc', 20, '2026-03-19T14:38:44.5019902Z', '33333333-3333-3333-3333-333333333333', '2026-03-19T14:38:44.5019903Z', '66666666-6666-6666-6666-666666666666'),
                ('dddddddd-dddd-dddd-dddd-dddddddddddd', 16, '2026-03-19T14:38:44.5019904Z', '33333333-3333-3333-3333-333333333333', '2026-03-19T14:38:44.5019904Z', '77777777-7777-7777-7777-777777777777'),
                ('eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee', 45, '2026-03-19T14:38:44.5019906Z', '33333333-3333-3333-3333-333333333333', '2026-03-19T14:38:44.5019906Z', '88888888-8888-8888-8888-888888888888'),
                ('ffffffff-ffff-ffff-ffff-ffffffffffff', 28, '2026-03-19T14:38:44.5019908Z', '33333333-3333-3333-3333-333333333333', '2026-03-19T14:38:44.5019908Z', '99999999-9999-9999-9999-999999999999');
                """);

            migrationBuilder.Sql("""
                CREATE INDEX IF NOT EXISTS "IX_ResourceAllocations_ProjectId"
                ON "ResourceAllocations" ("ProjectId");
                """);

            migrationBuilder.Sql("""
                CREATE UNIQUE INDEX IF NOT EXISTS "IX_ResourceAllocations_UserId_ProjectId"
                ON "ResourceAllocations" ("UserId", "ProjectId");
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ResourceAllocations");

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"));

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"));

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("66666666-6666-6666-6666-666666666666"));

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("77777777-7777-7777-7777-777777777777"));

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("88888888-8888-8888-8888-888888888888"));

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("99999999-9999-9999-9999-999999999999"));

            migrationBuilder.UpdateData(
                table: "Projects",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 18, 7, 51, 8, 910, DateTimeKind.Utc).AddTicks(3177), new DateTime(2026, 3, 18, 7, 51, 8, 910, DateTimeKind.Utc).AddTicks(3177) });

            migrationBuilder.UpdateData(
                table: "Tenants",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 18, 7, 51, 8, 910, DateTimeKind.Utc).AddTicks(3001), new DateTime(2026, 3, 18, 7, 51, 8, 910, DateTimeKind.Utc).AddTicks(3007) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "CreatedAt", "DisplayName", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 18, 7, 51, 8, 910, DateTimeKind.Utc).AddTicks(3151), "Berk-Can Ateşoğlu", new DateTime(2026, 3, 18, 7, 51, 8, 910, DateTimeKind.Utc).AddTicks(3151) });
        }
    }
}
