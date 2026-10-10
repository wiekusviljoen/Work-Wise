using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeaveWise.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationLeavePolicies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OrganizationLeavePolicies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "TEXT", nullable: false),
                    LeaveType = table.Column<string>(type: "TEXT", maxLength: 48, nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    EffectiveTo = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    CycleMonths = table.Column<int>(type: "INTEGER", nullable: false),
                    EntitlementDaysPerCycle = table.Column<decimal>(type: "TEXT", precision: 9, scale: 4, nullable: false),
                    AllowNegativeBalance = table.Column<bool>(type: "INTEGER", nullable: false),
                    AccrualMethod = table.Column<string>(type: "TEXT", maxLength: 24, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    ReviewReference = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    CreatedByUserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ActivatedByUserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true),
                    ActivatedUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationLeavePolicies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrganizationLeavePolicies_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LeaveLedger_EmploymentRecordId_EntryType_Reference",
                table: "LeaveLedger",
                columns: new[] { "EmploymentRecordId", "EntryType", "Reference" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationLeavePolicies_OrganizationId_LeaveType_Version",
                table: "OrganizationLeavePolicies",
                columns: new[] { "OrganizationId", "LeaveType", "Version" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrganizationLeavePolicies");

            migrationBuilder.DropIndex(
                name: "IX_LeaveLedger_EmploymentRecordId_EntryType_Reference",
                table: "LeaveLedger");
        }
    }
}
