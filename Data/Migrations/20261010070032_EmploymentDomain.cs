using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeaveWise.Migrations
{
    /// <inheritdoc />
    public partial class EmploymentDomain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ActorUserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true),
                    Action = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    EntityType = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    EntityId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    OccurredUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DetailsJson = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Organizations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    CountryCode = table.Column<string>(type: "TEXT", maxLength: 2, nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Organizations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmploymentRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EmployeeUserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true),
                    EmployeeName = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    EmployeeEmail = table.Column<string>(type: "TEXT", maxLength: 320, nullable: false),
                    CountryCode = table.Column<string>(type: "TEXT", maxLength: 2, nullable: false),
                    EmploymentStartDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    EmploymentEndDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    CurrencyCode = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    PayFrequency = table.Column<string>(type: "TEXT", maxLength: 24, nullable: false),
                    GrossSalary = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    WeeklyHours = table.Column<decimal>(type: "TEXT", precision: 6, scale: 2, nullable: false),
                    ScheduledWorkdaysMask = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 24, nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmploymentRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmploymentRecords_AspNetUsers_EmployeeUserId",
                        column: x => x.EmployeeUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_EmploymentRecords_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OrganizationMemberships",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                    Role = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    JoinedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationMemberships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrganizationMemberships_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrganizationMemberships_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BenefitReminders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    EmploymentRecordId = table.Column<Guid>(type: "TEXT", nullable: false),
                    BenefitType = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Label = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    ExpectedDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    ReminderDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    EligibilityStatus = table.Column<string>(type: "TEXT", maxLength: 48, nullable: false),
                    FormulaOrConditionsJson = table.Column<string>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BenefitReminders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BenefitReminders_EmploymentRecords_EmploymentRecordId",
                        column: x => x.EmploymentRecordId,
                        principalTable: "EmploymentRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmploymentContracts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    EmploymentRecordId = table.Column<Guid>(type: "TEXT", nullable: false),
                    OriginalFileName = table.Column<string>(type: "TEXT", maxLength: 260, nullable: false),
                    PrivateStorageKey = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    ExtractedFieldsJson = table.Column<string>(type: "TEXT", nullable: true),
                    UploadedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ReviewedUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ReviewedByUserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmploymentContracts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmploymentContracts_EmploymentRecords_EmploymentRecordId",
                        column: x => x.EmploymentRecordId,
                        principalTable: "EmploymentRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LeaveLedger",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    EmploymentRecordId = table.Column<Guid>(type: "TEXT", nullable: false),
                    LeaveType = table.Column<string>(type: "TEXT", maxLength: 48, nullable: false),
                    EffectiveDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    DayDelta = table.Column<decimal>(type: "TEXT", precision: 9, scale: 4, nullable: false),
                    EntryType = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Reference = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    CreatedByUserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ReversalOfId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveLedger", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LeaveLedger_EmploymentRecords_EmploymentRecordId",
                        column: x => x.EmploymentRecordId,
                        principalTable: "EmploymentRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LeaveRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    EmploymentRecordId = table.Column<Guid>(type: "TEXT", nullable: false),
                    LeaveType = table.Column<string>(type: "TEXT", maxLength: 48, nullable: false),
                    StartDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    ScheduledWorkdays = table.Column<decimal>(type: "TEXT", precision: 9, scale: 4, nullable: true),
                    EmployeeNote = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    RequestedByUserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                    ReviewedByUserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true),
                    RequestedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DecisionUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LeaveRequests_EmploymentRecords_EmploymentRecordId",
                        column: x => x.EmploymentRecordId,
                        principalTable: "EmploymentRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ContractClauses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    EmploymentContractId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ClauseType = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    ClauseText = table.Column<string>(type: "TEXT", nullable: false),
                    SourcePage = table.Column<int>(type: "INTEGER", nullable: true),
                    Confidence = table.Column<decimal>(type: "TEXT", precision: 5, scale: 4, nullable: true),
                    ReviewStatus = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    EntitlementDays = table.Column<decimal>(type: "TEXT", precision: 9, scale: 4, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContractClauses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContractClauses_EmploymentContracts_EmploymentContractId",
                        column: x => x.EmploymentContractId,
                        principalTable: "EmploymentContracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_OrganizationId_OccurredUtc",
                table: "AuditEvents",
                columns: new[] { "OrganizationId", "OccurredUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_BenefitReminders_EmploymentRecordId",
                table: "BenefitReminders",
                column: "EmploymentRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_ContractClauses_EmploymentContractId",
                table: "ContractClauses",
                column: "EmploymentContractId");

            migrationBuilder.CreateIndex(
                name: "IX_EmploymentContracts_EmploymentRecordId",
                table: "EmploymentContracts",
                column: "EmploymentRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_EmploymentRecords_EmployeeUserId",
                table: "EmploymentRecords",
                column: "EmployeeUserId");

            migrationBuilder.CreateIndex(
                name: "IX_EmploymentRecords_OrganizationId_EmployeeEmail",
                table: "EmploymentRecords",
                columns: new[] { "OrganizationId", "EmployeeEmail" });

            migrationBuilder.CreateIndex(
                name: "IX_LeaveLedger_EmploymentRecordId_LeaveType_EffectiveDate",
                table: "LeaveLedger",
                columns: new[] { "EmploymentRecordId", "LeaveType", "EffectiveDate" });

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequests_EmploymentRecordId_Status_StartDate",
                table: "LeaveRequests",
                columns: new[] { "EmploymentRecordId", "Status", "StartDate" });

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationMemberships_OrganizationId_UserId",
                table: "OrganizationMemberships",
                columns: new[] { "OrganizationId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationMemberships_UserId",
                table: "OrganizationMemberships",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_CountryCode_Name",
                table: "Organizations",
                columns: new[] { "CountryCode", "Name" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditEvents");

            migrationBuilder.DropTable(
                name: "BenefitReminders");

            migrationBuilder.DropTable(
                name: "ContractClauses");

            migrationBuilder.DropTable(
                name: "LeaveLedger");

            migrationBuilder.DropTable(
                name: "LeaveRequests");

            migrationBuilder.DropTable(
                name: "OrganizationMemberships");

            migrationBuilder.DropTable(
                name: "EmploymentContracts");

            migrationBuilder.DropTable(
                name: "EmploymentRecords");

            migrationBuilder.DropTable(
                name: "Organizations");
        }
    }
}
