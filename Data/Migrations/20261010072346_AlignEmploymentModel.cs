using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeaveWise.Migrations
{
    /// <inheritdoc />
    public partial class AlignEmploymentModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddForeignKey(
                name: "FK_AuditEvents_Organizations_OrganizationId",
                table: "AuditEvents",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AuditEvents_Organizations_OrganizationId",
                table: "AuditEvents");
        }
    }
}
