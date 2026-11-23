using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoachingPlatform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SemanticIntegrity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ClassSessions_ScheduleRuleId",
                table: "ClassSessions");

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "RefreshTokens",
                type: "bytea",
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_Gateway_GatewayTransactionId",
                table: "Payments",
                columns: new[] { "Gateway", "GatewayTransactionId" },
                unique: true,
                filter: "\"GatewayTransactionId\" IS NOT NULL AND \"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_ClassSessions_ScheduleRuleId_StartUtc",
                table: "ClassSessions",
                columns: new[] { "ScheduleRuleId", "StartUtc" },
                unique: true,
                filter: "\"ScheduleRuleId\" IS NOT NULL AND \"IsDeleted\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Payments_Gateway_GatewayTransactionId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_ClassSessions_ScheduleRuleId_StartUtc",
                table: "ClassSessions");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "RefreshTokens");

            migrationBuilder.CreateIndex(
                name: "IX_ClassSessions_ScheduleRuleId",
                table: "ClassSessions",
                column: "ScheduleRuleId");
        }
    }
}
