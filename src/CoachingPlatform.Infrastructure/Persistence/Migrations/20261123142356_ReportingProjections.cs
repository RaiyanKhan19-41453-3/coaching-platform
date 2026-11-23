using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoachingPlatform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ReportingProjections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AttendanceStats",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    BatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    Period = table.Column<DateOnly>(type: "date", nullable: false),
                    Sessions = table.Column<int>(type: "integer", nullable: false),
                    Present = table.Column<int>(type: "integer", nullable: false),
                    Absent = table.Column<int>(type: "integer", nullable: false),
                    Late = table.Column<int>(type: "integer", nullable: false),
                    Excused = table.Column<int>(type: "integer", nullable: false),
                    AttendancePercent = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendanceStats", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BatchUtilizations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    Capacity = table.Column<int>(type: "integer", nullable: false),
                    Enrolled = table.Column<int>(type: "integer", nullable: false),
                    Waitlisted = table.Column<int>(type: "integer", nullable: false),
                    UtilizationPercent = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BatchUtilizations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FeeSummaries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Period = table.Column<DateOnly>(type: "date", nullable: false),
                    Invoiced_Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Invoiced_Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Paid_Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Paid_Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Outstanding_Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Outstanding_Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    InvoiceCount = table.Column<int>(type: "integer", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeeSummaries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GradeSummaries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    Average = table.Column<decimal>(type: "numeric(6,2)", nullable: false),
                    Highest = table.Column<decimal>(type: "numeric", nullable: false),
                    Lowest = table.Column<decimal>(type: "numeric", nullable: false),
                    Assessments = table.Column<int>(type: "integer", nullable: false),
                    MarksObtained = table.Column<decimal>(type: "numeric", nullable: false),
                    MaxMarks = table.Column<decimal>(type: "numeric", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GradeSummaries", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceStats_TenantId_StudentId_BatchId_Period",
                table: "AttendanceStats",
                columns: new[] { "TenantId", "StudentId", "BatchId", "Period" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_BatchUtilizations_TenantId_BatchId",
                table: "BatchUtilizations",
                columns: new[] { "TenantId", "BatchId" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_FeeSummaries_TenantId_StudentId_Period",
                table: "FeeSummaries",
                columns: new[] { "TenantId", "StudentId", "Period" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_GradeSummaries_TenantId_StudentId_SubjectId",
                table: "GradeSummaries",
                columns: new[] { "TenantId", "StudentId", "SubjectId" },
                unique: true,
                filter: "\"IsDeleted\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AttendanceStats");

            migrationBuilder.DropTable(
                name: "BatchUtilizations");

            migrationBuilder.DropTable(
                name: "FeeSummaries");

            migrationBuilder.DropTable(
                name: "GradeSummaries");
        }
    }
}
