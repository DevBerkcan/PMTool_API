using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PmTool.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddContactsAndNoteExtensions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "ProjectNotes",
                type: "TEXT",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsPinned",
                table: "ProjectNotes",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateOnly>(
                name: "MeetingDate",
                table: "ProjectNotes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Participants",
                table: "ProjectNotes",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "ProjectContacts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProjectId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Phone = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Company = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Role = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Supervisor = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectContacts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectContacts_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "ProjectDecisions",
                keyColumn: "Id",
                keyValue: new Guid("81818181-8181-8181-8181-818181818181"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 7, 11, 28, 33, 475, DateTimeKind.Utc).AddTicks(1096), new DateTime(2026, 4, 7, 11, 28, 33, 475, DateTimeKind.Utc).AddTicks(1096) });

            migrationBuilder.UpdateData(
                table: "ProjectDecisions",
                keyColumn: "Id",
                keyValue: new Guid("82828282-8282-8282-8282-828282828282"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 7, 11, 28, 33, 475, DateTimeKind.Utc).AddTicks(1101), new DateTime(2026, 4, 7, 11, 28, 33, 475, DateTimeKind.Utc).AddTicks(1102) });

            migrationBuilder.UpdateData(
                table: "ProjectDecisions",
                keyColumn: "Id",
                keyValue: new Guid("83838383-8383-8383-8383-838383838383"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 7, 11, 28, 33, 475, DateTimeKind.Utc).AddTicks(1106), new DateTime(2026, 4, 7, 11, 28, 33, 475, DateTimeKind.Utc).AddTicks(1106) });

            migrationBuilder.UpdateData(
                table: "ProjectDecisions",
                keyColumn: "Id",
                keyValue: new Guid("84848484-8484-8484-8484-848484848484"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 7, 11, 28, 33, 475, DateTimeKind.Utc).AddTicks(1111), new DateTime(2026, 4, 7, 11, 28, 33, 475, DateTimeKind.Utc).AddTicks(1111) });

            migrationBuilder.UpdateData(
                table: "ProjectDecisions",
                keyColumn: "Id",
                keyValue: new Guid("85858585-8585-8585-8585-858585858585"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 7, 11, 28, 33, 475, DateTimeKind.Utc).AddTicks(1114), new DateTime(2026, 4, 7, 11, 28, 33, 475, DateTimeKind.Utc).AddTicks(1114) });

            migrationBuilder.UpdateData(
                table: "ProjectDocuments",
                keyColumn: "Id",
                keyValue: new Guid("91919191-9191-9191-9191-919191919191"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 7, 11, 28, 33, 475, DateTimeKind.Utc).AddTicks(1135), new DateTime(2026, 4, 7, 11, 28, 33, 475, DateTimeKind.Utc).AddTicks(1135) });

            migrationBuilder.UpdateData(
                table: "ProjectDocuments",
                keyColumn: "Id",
                keyValue: new Guid("92929292-9292-9292-9292-929292929292"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 7, 11, 28, 33, 475, DateTimeKind.Utc).AddTicks(1138), new DateTime(2026, 4, 7, 11, 28, 33, 475, DateTimeKind.Utc).AddTicks(1139) });

            migrationBuilder.UpdateData(
                table: "ProjectDocuments",
                keyColumn: "Id",
                keyValue: new Guid("93939393-9393-9393-9393-939393939393"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 7, 11, 28, 33, 475, DateTimeKind.Utc).AddTicks(1142), new DateTime(2026, 4, 7, 11, 28, 33, 475, DateTimeKind.Utc).AddTicks(1143) });

            migrationBuilder.UpdateData(
                table: "ProjectDocuments",
                keyColumn: "Id",
                keyValue: new Guid("94949494-9494-9494-9494-949494949494"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 7, 11, 28, 33, 475, DateTimeKind.Utc).AddTicks(1145), new DateTime(2026, 4, 7, 11, 28, 33, 475, DateTimeKind.Utc).AddTicks(1145) });

            migrationBuilder.UpdateData(
                table: "ProjectDocuments",
                keyColumn: "Id",
                keyValue: new Guid("95959595-9595-9595-9595-959595959595"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 7, 11, 28, 33, 475, DateTimeKind.Utc).AddTicks(1147), new DateTime(2026, 4, 7, 11, 28, 33, 475, DateTimeKind.Utc).AddTicks(1147) });

            migrationBuilder.UpdateData(
                table: "ProjectGovernanceChecks",
                keyColumn: "Id",
                keyValue: new Guid("a1a1a1a1-a1a1-a1a1-a1a1-a1a1a1a1a1a1"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 7, 11, 28, 33, 475, DateTimeKind.Utc).AddTicks(1170), new DateTime(2026, 4, 7, 11, 28, 33, 475, DateTimeKind.Utc).AddTicks(1170) });

            migrationBuilder.UpdateData(
                table: "ProjectGovernanceChecks",
                keyColumn: "Id",
                keyValue: new Guid("a2a2a2a2-a2a2-a2a2-a2a2-a2a2a2a2a2a2"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 7, 11, 28, 33, 475, DateTimeKind.Utc).AddTicks(1176), new DateTime(2026, 4, 7, 11, 28, 33, 475, DateTimeKind.Utc).AddTicks(1176) });

            migrationBuilder.UpdateData(
                table: "ProjectGovernanceChecks",
                keyColumn: "Id",
                keyValue: new Guid("a3a3a3a3-a3a3-a3a3-a3a3-a3a3a3a3a3a3"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 7, 11, 28, 33, 475, DateTimeKind.Utc).AddTicks(1178), new DateTime(2026, 4, 7, 11, 28, 33, 475, DateTimeKind.Utc).AddTicks(1179) });

            migrationBuilder.UpdateData(
                table: "ProjectGovernanceChecks",
                keyColumn: "Id",
                keyValue: new Guid("a4a4a4a4-a4a4-a4a4-a4a4-a4a4a4a4a4a4"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 7, 11, 28, 33, 475, DateTimeKind.Utc).AddTicks(1181), new DateTime(2026, 4, 7, 11, 28, 33, 475, DateTimeKind.Utc).AddTicks(1181) });

            migrationBuilder.UpdateData(
                table: "ProjectGovernanceChecks",
                keyColumn: "Id",
                keyValue: new Guid("a5a5a5a5-a5a5-a5a5-a5a5-a5a5a5a5a5a5"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 7, 11, 28, 33, 475, DateTimeKind.Utc).AddTicks(1183), new DateTime(2026, 4, 7, 11, 28, 33, 475, DateTimeKind.Utc).AddTicks(1184) });

            migrationBuilder.UpdateData(
                table: "ProjectLeadTasks",
                keyColumn: "Id",
                keyValue: new Guid("19191919-1919-1919-1919-191919191919"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 7, 11, 28, 33, 475, DateTimeKind.Utc).AddTicks(1018), new DateTime(2026, 4, 7, 11, 28, 33, 475, DateTimeKind.Utc).AddTicks(1018) });

            migrationBuilder.UpdateData(
                table: "ProjectLeadTasks",
                keyColumn: "Id",
                keyValue: new Guid("20202020-2020-2020-2020-202020202020"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 7, 11, 28, 33, 475, DateTimeKind.Utc).AddTicks(1027), new DateTime(2026, 4, 7, 11, 28, 33, 475, DateTimeKind.Utc).AddTicks(1027) });

            migrationBuilder.UpdateData(
                table: "ProjectLeadTasks",
                keyColumn: "Id",
                keyValue: new Guid("21212121-2121-2121-2121-212121212121"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 7, 11, 28, 33, 475, DateTimeKind.Utc).AddTicks(1030), new DateTime(2026, 4, 7, 11, 28, 33, 475, DateTimeKind.Utc).AddTicks(1030) });

            migrationBuilder.UpdateData(
                table: "ProjectLeadTasks",
                keyColumn: "Id",
                keyValue: new Guid("22222221-2222-2222-2222-222222222221"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 7, 11, 28, 33, 475, DateTimeKind.Utc).AddTicks(1032), new DateTime(2026, 4, 7, 11, 28, 33, 475, DateTimeKind.Utc).AddTicks(1032) });

            migrationBuilder.UpdateData(
                table: "ProjectLeadTasks",
                keyColumn: "Id",
                keyValue: new Guid("23232323-2323-2323-2323-232323232323"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 7, 11, 28, 33, 475, DateTimeKind.Utc).AddTicks(1035), new DateTime(2026, 4, 7, 11, 28, 33, 475, DateTimeKind.Utc).AddTicks(1036) });

            migrationBuilder.UpdateData(
                table: "ProjectLeadTasks",
                keyColumn: "Id",
                keyValue: new Guid("24242424-2424-2424-2424-242424242424"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 7, 11, 28, 33, 475, DateTimeKind.Utc).AddTicks(1038), new DateTime(2026, 4, 7, 11, 28, 33, 475, DateTimeKind.Utc).AddTicks(1038) });

            migrationBuilder.UpdateData(
                table: "ProjectLeadTasks",
                keyColumn: "Id",
                keyValue: new Guid("25252525-2525-2525-2525-252525252525"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 7, 11, 28, 33, 475, DateTimeKind.Utc).AddTicks(1040), new DateTime(2026, 4, 7, 11, 28, 33, 475, DateTimeKind.Utc).AddTicks(1040) });

            migrationBuilder.UpdateData(
                table: "ProjectLeadTasks",
                keyColumn: "Id",
                keyValue: new Guid("26262626-2626-2626-2626-262626262626"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 7, 11, 28, 33, 475, DateTimeKind.Utc).AddTicks(1042), new DateTime(2026, 4, 7, 11, 28, 33, 475, DateTimeKind.Utc).AddTicks(1042) });

            migrationBuilder.UpdateData(
                table: "ProjectMilestones",
                keyColumn: "Id",
                keyValue: new Guid("71717171-7171-7171-7171-717171717171"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 7, 11, 28, 33, 475, DateTimeKind.Utc).AddTicks(1063), new DateTime(2026, 4, 7, 11, 28, 33, 475, DateTimeKind.Utc).AddTicks(1063) });

            migrationBuilder.UpdateData(
                table: "ProjectMilestones",
                keyColumn: "Id",
                keyValue: new Guid("72727272-7272-7272-7272-727272727272"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 7, 11, 28, 33, 475, DateTimeKind.Utc).AddTicks(1068), new DateTime(2026, 4, 7, 11, 28, 33, 475, DateTimeKind.Utc).AddTicks(1068) });

            migrationBuilder.UpdateData(
                table: "ProjectMilestones",
                keyColumn: "Id",
                keyValue: new Guid("73737373-7373-7373-7373-737373737373"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 7, 11, 28, 33, 475, DateTimeKind.Utc).AddTicks(1070), new DateTime(2026, 4, 7, 11, 28, 33, 475, DateTimeKind.Utc).AddTicks(1070) });

            migrationBuilder.UpdateData(
                table: "ProjectMilestones",
                keyColumn: "Id",
                keyValue: new Guid("74747474-7474-7474-7474-747474747474"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 7, 11, 28, 33, 475, DateTimeKind.Utc).AddTicks(1072), new DateTime(2026, 4, 7, 11, 28, 33, 475, DateTimeKind.Utc).AddTicks(1072) });

            migrationBuilder.UpdateData(
                table: "ProjectMilestones",
                keyColumn: "Id",
                keyValue: new Guid("75757575-7575-7575-7575-757575757575"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 7, 11, 28, 33, 475, DateTimeKind.Utc).AddTicks(1076), new DateTime(2026, 4, 7, 11, 28, 33, 475, DateTimeKind.Utc).AddTicks(1076) });

            migrationBuilder.UpdateData(
                table: "ProjectNotes",
                keyColumn: "Id",
                keyValue: new Guid("16161616-1616-1616-1616-161616161616"),
                columns: new[] { "Category", "IsPinned", "MeetingDate", "Participants" },
                values: new object[] { "general", false, null, "" });

            migrationBuilder.UpdateData(
                table: "ProjectNotes",
                keyColumn: "Id",
                keyValue: new Guid("17171717-1717-1717-1717-171717171717"),
                columns: new[] { "Category", "IsPinned", "MeetingDate", "Participants" },
                values: new object[] { "general", false, null, "" });

            migrationBuilder.UpdateData(
                table: "ProjectNotes",
                keyColumn: "Id",
                keyValue: new Guid("18181818-1818-1818-1818-181818181818"),
                columns: new[] { "Category", "IsPinned", "MeetingDate", "Participants" },
                values: new object[] { "general", false, null, "" });

            migrationBuilder.UpdateData(
                table: "ProjectNotes",
                keyColumn: "Id",
                keyValue: new Guid("19181818-1818-1818-1818-181818181819"),
                columns: new[] { "Category", "IsPinned", "MeetingDate", "Participants" },
                values: new object[] { "general", false, null, "" });

            migrationBuilder.UpdateData(
                table: "ProjectNotes",
                keyColumn: "Id",
                keyValue: new Guid("20282828-2828-2828-2828-282828282828"),
                columns: new[] { "Category", "IsPinned", "MeetingDate", "Participants" },
                values: new object[] { "general", false, null, "" });

            migrationBuilder.UpdateData(
                table: "ProjectNotes",
                keyColumn: "Id",
                keyValue: new Guid("30383838-3838-3838-3838-383838383838"),
                columns: new[] { "Category", "IsPinned", "MeetingDate", "Participants" },
                values: new object[] { "general", false, null, "" });

            migrationBuilder.UpdateData(
                table: "ProjectTeamsLinks",
                keyColumn: "Id",
                keyValue: new Guid("d1d1d1d1-d1d1-d1d1-d1d1-d1d1d1d1d1d1"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 7, 11, 28, 33, 475, DateTimeKind.Utc).AddTicks(1334), new DateTime(2026, 4, 7, 11, 28, 33, 475, DateTimeKind.Utc).AddTicks(1334) });

            migrationBuilder.UpdateData(
                table: "ProjectTeamsLinks",
                keyColumn: "Id",
                keyValue: new Guid("d2d2d2d2-d2d2-d2d2-d2d2-d2d2d2d2d2d2"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 7, 11, 28, 33, 475, DateTimeKind.Utc).AddTicks(1337), new DateTime(2026, 4, 7, 11, 28, 33, 475, DateTimeKind.Utc).AddTicks(1337) });

            migrationBuilder.UpdateData(
                table: "Projects",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 7, 11, 28, 33, 475, DateTimeKind.Utc).AddTicks(707), new DateTime(2026, 4, 7, 11, 28, 33, 475, DateTimeKind.Utc).AddTicks(708) });

            migrationBuilder.UpdateData(
                table: "Projects",
                keyColumn: "Id",
                keyValue: new Guid("34343434-3434-3434-3434-343434343434"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 7, 11, 28, 33, 475, DateTimeKind.Utc).AddTicks(758), new DateTime(2026, 4, 7, 11, 28, 33, 475, DateTimeKind.Utc).AddTicks(758) });

            migrationBuilder.UpdateData(
                table: "Projects",
                keyColumn: "Id",
                keyValue: new Guid("45454545-4545-4545-4545-454545454545"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 7, 11, 28, 33, 475, DateTimeKind.Utc).AddTicks(763), new DateTime(2026, 4, 7, 11, 28, 33, 475, DateTimeKind.Utc).AddTicks(764) });

            migrationBuilder.UpdateData(
                table: "Projects",
                keyColumn: "Id",
                keyValue: new Guid("56565656-5656-5656-5656-565656565656"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 7, 11, 28, 33, 475, DateTimeKind.Utc).AddTicks(768), new DateTime(2026, 4, 7, 11, 28, 33, 475, DateTimeKind.Utc).AddTicks(769) });

            migrationBuilder.UpdateData(
                table: "Projects",
                keyColumn: "Id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 7, 11, 28, 33, 475, DateTimeKind.Utc).AddTicks(751), new DateTime(2026, 4, 7, 11, 28, 33, 475, DateTimeKind.Utc).AddTicks(751) });

            migrationBuilder.UpdateData(
                table: "ResourceAllocations",
                keyColumn: "Id",
                keyValue: new Guid("12121212-1212-1212-1212-121212121212"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 7, 11, 28, 33, 475, DateTimeKind.Utc).AddTicks(815), new DateTime(2026, 4, 7, 11, 28, 33, 475, DateTimeKind.Utc).AddTicks(816) });

            migrationBuilder.UpdateData(
                table: "ResourceAllocations",
                keyColumn: "Id",
                keyValue: new Guid("13131313-1313-1313-1313-131313131313"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 7, 11, 28, 33, 475, DateTimeKind.Utc).AddTicks(818), new DateTime(2026, 4, 7, 11, 28, 33, 475, DateTimeKind.Utc).AddTicks(818) });

            migrationBuilder.UpdateData(
                table: "ResourceAllocations",
                keyColumn: "Id",
                keyValue: new Guid("14141414-1414-1414-1414-141414141414"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 7, 11, 28, 33, 475, DateTimeKind.Utc).AddTicks(820), new DateTime(2026, 4, 7, 11, 28, 33, 475, DateTimeKind.Utc).AddTicks(820) });

            migrationBuilder.UpdateData(
                table: "ResourceAllocations",
                keyColumn: "Id",
                keyValue: new Guid("15151515-1515-1515-1515-151515151515"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 7, 11, 28, 33, 475, DateTimeKind.Utc).AddTicks(822), new DateTime(2026, 4, 7, 11, 28, 33, 475, DateTimeKind.Utc).AddTicks(822) });

            migrationBuilder.UpdateData(
                table: "ResourceAllocations",
                keyColumn: "Id",
                keyValue: new Guid("61616161-6161-6161-6161-616161616161"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 7, 11, 28, 33, 475, DateTimeKind.Utc).AddTicks(823), new DateTime(2026, 4, 7, 11, 28, 33, 475, DateTimeKind.Utc).AddTicks(824) });

            migrationBuilder.UpdateData(
                table: "ResourceAllocations",
                keyColumn: "Id",
                keyValue: new Guid("62626262-6262-6262-6262-626262626262"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 7, 11, 28, 33, 475, DateTimeKind.Utc).AddTicks(825), new DateTime(2026, 4, 7, 11, 28, 33, 475, DateTimeKind.Utc).AddTicks(825) });

            migrationBuilder.UpdateData(
                table: "ResourceAllocations",
                keyColumn: "Id",
                keyValue: new Guid("63636363-6363-6363-6363-636363636363"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 7, 11, 28, 33, 475, DateTimeKind.Utc).AddTicks(827), new DateTime(2026, 4, 7, 11, 28, 33, 475, DateTimeKind.Utc).AddTicks(827) });

            migrationBuilder.UpdateData(
                table: "ResourceAllocations",
                keyColumn: "Id",
                keyValue: new Guid("64646464-6464-6464-6464-646464646464"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 7, 11, 28, 33, 475, DateTimeKind.Utc).AddTicks(832), new DateTime(2026, 4, 7, 11, 28, 33, 475, DateTimeKind.Utc).AddTicks(832) });

            migrationBuilder.UpdateData(
                table: "ResourceAllocations",
                keyColumn: "Id",
                keyValue: new Guid("65656565-6565-6565-6565-656565656565"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 7, 11, 28, 33, 475, DateTimeKind.Utc).AddTicks(834), new DateTime(2026, 4, 7, 11, 28, 33, 475, DateTimeKind.Utc).AddTicks(834) });

            migrationBuilder.UpdateData(
                table: "ResourceAllocations",
                keyColumn: "Id",
                keyValue: new Guid("66666661-6666-6666-6666-666666666661"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 7, 11, 28, 33, 475, DateTimeKind.Utc).AddTicks(836), new DateTime(2026, 4, 7, 11, 28, 33, 475, DateTimeKind.Utc).AddTicks(836) });

            migrationBuilder.UpdateData(
                table: "ResourceAllocations",
                keyColumn: "Id",
                keyValue: new Guid("67676767-6767-6767-6767-676767676767"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 7, 11, 28, 33, 475, DateTimeKind.Utc).AddTicks(838), new DateTime(2026, 4, 7, 11, 28, 33, 475, DateTimeKind.Utc).AddTicks(838) });

            migrationBuilder.UpdateData(
                table: "ResourceAllocations",
                keyColumn: "Id",
                keyValue: new Guid("68686868-6868-6868-6868-686868686868"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 7, 11, 28, 33, 475, DateTimeKind.Utc).AddTicks(840), new DateTime(2026, 4, 7, 11, 28, 33, 475, DateTimeKind.Utc).AddTicks(840) });

            migrationBuilder.UpdateData(
                table: "ResourceAllocations",
                keyColumn: "Id",
                keyValue: new Guid("69696969-6969-6969-6969-696969696969"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 7, 11, 28, 33, 475, DateTimeKind.Utc).AddTicks(842), new DateTime(2026, 4, 7, 11, 28, 33, 475, DateTimeKind.Utc).AddTicks(842) });

            migrationBuilder.UpdateData(
                table: "ResourceAllocations",
                keyColumn: "Id",
                keyValue: new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 7, 11, 28, 33, 475, DateTimeKind.Utc).AddTicks(797), new DateTime(2026, 4, 7, 11, 28, 33, 475, DateTimeKind.Utc).AddTicks(797) });

            migrationBuilder.UpdateData(
                table: "ResourceAllocations",
                keyColumn: "Id",
                keyValue: new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 7, 11, 28, 33, 475, DateTimeKind.Utc).AddTicks(805), new DateTime(2026, 4, 7, 11, 28, 33, 475, DateTimeKind.Utc).AddTicks(806) });

            migrationBuilder.UpdateData(
                table: "ResourceAllocations",
                keyColumn: "Id",
                keyValue: new Guid("dddddddd-dddd-dddd-dddd-dddddddddddd"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 7, 11, 28, 33, 475, DateTimeKind.Utc).AddTicks(807), new DateTime(2026, 4, 7, 11, 28, 33, 475, DateTimeKind.Utc).AddTicks(808) });

            migrationBuilder.UpdateData(
                table: "ResourceAllocations",
                keyColumn: "Id",
                keyValue: new Guid("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 7, 11, 28, 33, 475, DateTimeKind.Utc).AddTicks(809), new DateTime(2026, 4, 7, 11, 28, 33, 475, DateTimeKind.Utc).AddTicks(809) });

            migrationBuilder.UpdateData(
                table: "ResourceAllocations",
                keyColumn: "Id",
                keyValue: new Guid("ffffffff-ffff-ffff-ffff-ffffffffffff"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 7, 11, 28, 33, 475, DateTimeKind.Utc).AddTicks(814), new DateTime(2026, 4, 7, 11, 28, 33, 475, DateTimeKind.Utc).AddTicks(814) });

            migrationBuilder.UpdateData(
                table: "Tenants",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 7, 11, 28, 33, 475, DateTimeKind.Utc).AddTicks(386), new DateTime(2026, 4, 7, 11, 28, 33, 475, DateTimeKind.Utc).AddTicks(392) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("10101010-1010-1010-1010-101010101010"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 7, 11, 28, 33, 475, DateTimeKind.Utc).AddTicks(602), new DateTime(2026, 4, 7, 11, 28, 33, 475, DateTimeKind.Utc).AddTicks(602) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 7, 11, 28, 33, 475, DateTimeKind.Utc).AddTicks(573), new DateTime(2026, 4, 7, 11, 28, 33, 475, DateTimeKind.Utc).AddTicks(573) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 7, 11, 28, 33, 475, DateTimeKind.Utc).AddTicks(591), new DateTime(2026, 4, 7, 11, 28, 33, 475, DateTimeKind.Utc).AddTicks(592) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 7, 11, 28, 33, 475, DateTimeKind.Utc).AddTicks(593), new DateTime(2026, 4, 7, 11, 28, 33, 475, DateTimeKind.Utc).AddTicks(594) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("66666666-6666-6666-6666-666666666666"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 7, 11, 28, 33, 475, DateTimeKind.Utc).AddTicks(595), new DateTime(2026, 4, 7, 11, 28, 33, 475, DateTimeKind.Utc).AddTicks(595) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("77777777-7777-7777-7777-777777777777"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 7, 11, 28, 33, 475, DateTimeKind.Utc).AddTicks(597), new DateTime(2026, 4, 7, 11, 28, 33, 475, DateTimeKind.Utc).AddTicks(597) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("88888888-8888-8888-8888-888888888888"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 7, 11, 28, 33, 475, DateTimeKind.Utc).AddTicks(598), new DateTime(2026, 4, 7, 11, 28, 33, 475, DateTimeKind.Utc).AddTicks(599) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("99999999-9999-9999-9999-999999999999"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 7, 11, 28, 33, 475, DateTimeKind.Utc).AddTicks(600), new DateTime(2026, 4, 7, 11, 28, 33, 475, DateTimeKind.Utc).AddTicks(600) });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectContacts_ProjectId",
                table: "ProjectContacts",
                column: "ProjectId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProjectContacts");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "ProjectNotes");

            migrationBuilder.DropColumn(
                name: "IsPinned",
                table: "ProjectNotes");

            migrationBuilder.DropColumn(
                name: "MeetingDate",
                table: "ProjectNotes");

            migrationBuilder.DropColumn(
                name: "Participants",
                table: "ProjectNotes");

            migrationBuilder.UpdateData(
                table: "ProjectDecisions",
                keyColumn: "Id",
                keyValue: new Guid("81818181-8181-8181-8181-818181818181"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4909), new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4909) });

            migrationBuilder.UpdateData(
                table: "ProjectDecisions",
                keyColumn: "Id",
                keyValue: new Guid("82828282-8282-8282-8282-828282828282"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4915), new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4915) });

            migrationBuilder.UpdateData(
                table: "ProjectDecisions",
                keyColumn: "Id",
                keyValue: new Guid("83838383-8383-8383-8383-838383838383"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4917), new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4917) });

            migrationBuilder.UpdateData(
                table: "ProjectDecisions",
                keyColumn: "Id",
                keyValue: new Guid("84848484-8484-8484-8484-848484848484"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4919), new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4919) });

            migrationBuilder.UpdateData(
                table: "ProjectDecisions",
                keyColumn: "Id",
                keyValue: new Guid("85858585-8585-8585-8585-858585858585"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4921), new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4922) });

            migrationBuilder.UpdateData(
                table: "ProjectDocuments",
                keyColumn: "Id",
                keyValue: new Guid("91919191-9191-9191-9191-919191919191"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4939), new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4939) });

            migrationBuilder.UpdateData(
                table: "ProjectDocuments",
                keyColumn: "Id",
                keyValue: new Guid("92929292-9292-9292-9292-929292929292"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4942), new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4942) });

            migrationBuilder.UpdateData(
                table: "ProjectDocuments",
                keyColumn: "Id",
                keyValue: new Guid("93939393-9393-9393-9393-939393939393"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4944), new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4944) });

            migrationBuilder.UpdateData(
                table: "ProjectDocuments",
                keyColumn: "Id",
                keyValue: new Guid("94949494-9494-9494-9494-949494949494"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4945), new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4946) });

            migrationBuilder.UpdateData(
                table: "ProjectDocuments",
                keyColumn: "Id",
                keyValue: new Guid("95959595-9595-9595-9595-959595959595"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4949), new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4949) });

            migrationBuilder.UpdateData(
                table: "ProjectGovernanceChecks",
                keyColumn: "Id",
                keyValue: new Guid("a1a1a1a1-a1a1-a1a1-a1a1-a1a1a1a1a1a1"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4964), new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4964) });

            migrationBuilder.UpdateData(
                table: "ProjectGovernanceChecks",
                keyColumn: "Id",
                keyValue: new Guid("a2a2a2a2-a2a2-a2a2-a2a2-a2a2a2a2a2a2"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4967), new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4967) });

            migrationBuilder.UpdateData(
                table: "ProjectGovernanceChecks",
                keyColumn: "Id",
                keyValue: new Guid("a3a3a3a3-a3a3-a3a3-a3a3-a3a3a3a3a3a3"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4969), new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4969) });

            migrationBuilder.UpdateData(
                table: "ProjectGovernanceChecks",
                keyColumn: "Id",
                keyValue: new Guid("a4a4a4a4-a4a4-a4a4-a4a4-a4a4a4a4a4a4"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4971), new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4971) });

            migrationBuilder.UpdateData(
                table: "ProjectGovernanceChecks",
                keyColumn: "Id",
                keyValue: new Guid("a5a5a5a5-a5a5-a5a5-a5a5-a5a5a5a5a5a5"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4973), new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4973) });

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

            migrationBuilder.UpdateData(
                table: "ProjectLeadTasks",
                keyColumn: "Id",
                keyValue: new Guid("24242424-2424-2424-2424-242424242424"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4856), new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4857) });

            migrationBuilder.UpdateData(
                table: "ProjectLeadTasks",
                keyColumn: "Id",
                keyValue: new Guid("25252525-2525-2525-2525-252525252525"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4860), new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4860) });

            migrationBuilder.UpdateData(
                table: "ProjectLeadTasks",
                keyColumn: "Id",
                keyValue: new Guid("26262626-2626-2626-2626-262626262626"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4862), new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4862) });

            migrationBuilder.UpdateData(
                table: "ProjectMilestones",
                keyColumn: "Id",
                keyValue: new Guid("71717171-7171-7171-7171-717171717171"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4881), new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4881) });

            migrationBuilder.UpdateData(
                table: "ProjectMilestones",
                keyColumn: "Id",
                keyValue: new Guid("72727272-7272-7272-7272-727272727272"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4884), new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4884) });

            migrationBuilder.UpdateData(
                table: "ProjectMilestones",
                keyColumn: "Id",
                keyValue: new Guid("73737373-7373-7373-7373-737373737373"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4886), new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4887) });

            migrationBuilder.UpdateData(
                table: "ProjectMilestones",
                keyColumn: "Id",
                keyValue: new Guid("74747474-7474-7474-7474-747474747474"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4888), new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4888) });

            migrationBuilder.UpdateData(
                table: "ProjectMilestones",
                keyColumn: "Id",
                keyValue: new Guid("75757575-7575-7575-7575-757575757575"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4890), new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4890) });

            migrationBuilder.UpdateData(
                table: "ProjectTeamsLinks",
                keyColumn: "Id",
                keyValue: new Guid("d1d1d1d1-d1d1-d1d1-d1d1-d1d1d1d1d1d1"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(5131), new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(5131) });

            migrationBuilder.UpdateData(
                table: "ProjectTeamsLinks",
                keyColumn: "Id",
                keyValue: new Guid("d2d2d2d2-d2d2-d2d2-d2d2-d2d2d2d2d2d2"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(5135), new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(5135) });

            migrationBuilder.UpdateData(
                table: "Projects",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4568), new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4569) });

            migrationBuilder.UpdateData(
                table: "Projects",
                keyColumn: "Id",
                keyValue: new Guid("34343434-3434-3434-3434-343434343434"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4601), new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4601) });

            migrationBuilder.UpdateData(
                table: "Projects",
                keyColumn: "Id",
                keyValue: new Guid("45454545-4545-4545-4545-454545454545"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4608), new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4608) });

            migrationBuilder.UpdateData(
                table: "Projects",
                keyColumn: "Id",
                keyValue: new Guid("56565656-5656-5656-5656-565656565656"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4612), new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4613) });

            migrationBuilder.UpdateData(
                table: "Projects",
                keyColumn: "Id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4595), new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4596) });

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
                keyValue: new Guid("61616161-6161-6161-6161-616161616161"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4670), new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4671) });

            migrationBuilder.UpdateData(
                table: "ResourceAllocations",
                keyColumn: "Id",
                keyValue: new Guid("62626262-6262-6262-6262-626262626262"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4672), new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4673) });

            migrationBuilder.UpdateData(
                table: "ResourceAllocations",
                keyColumn: "Id",
                keyValue: new Guid("63636363-6363-6363-6363-636363636363"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4674), new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4674) });

            migrationBuilder.UpdateData(
                table: "ResourceAllocations",
                keyColumn: "Id",
                keyValue: new Guid("64646464-6464-6464-6464-646464646464"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4676), new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4676) });

            migrationBuilder.UpdateData(
                table: "ResourceAllocations",
                keyColumn: "Id",
                keyValue: new Guid("65656565-6565-6565-6565-656565656565"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4678), new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4678) });

            migrationBuilder.UpdateData(
                table: "ResourceAllocations",
                keyColumn: "Id",
                keyValue: new Guid("66666661-6666-6666-6666-666666666661"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4682), new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4682) });

            migrationBuilder.UpdateData(
                table: "ResourceAllocations",
                keyColumn: "Id",
                keyValue: new Guid("67676767-6767-6767-6767-676767676767"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4683), new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4684) });

            migrationBuilder.UpdateData(
                table: "ResourceAllocations",
                keyColumn: "Id",
                keyValue: new Guid("68686868-6868-6868-6868-686868686868"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4685), new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4685) });

            migrationBuilder.UpdateData(
                table: "ResourceAllocations",
                keyColumn: "Id",
                keyValue: new Guid("69696969-6969-6969-6969-696969696969"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4687), new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4687) });

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
                keyValue: new Guid("10101010-1010-1010-1010-101010101010"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4448), new DateTime(2026, 4, 7, 7, 54, 1, 256, DateTimeKind.Utc).AddTicks(4448) });

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
        }
    }
}
