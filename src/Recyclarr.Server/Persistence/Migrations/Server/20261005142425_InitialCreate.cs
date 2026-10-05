using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServerDatabaseMigrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SyncJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Trigger = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    StartedAt = table.Column<long>(type: "INTEGER", nullable: true),
                    FinishedAt = table.Column<long>(type: "INTEGER", nullable: true),
                    ScheduledFor = table.Column<long>(type: "INTEGER", nullable: true),
                    Service = table.Column<string>(type: "TEXT", nullable: true),
                    Instances = table.Column<string>(type: "TEXT", nullable: false),
                    Preview = table.Column<bool>(type: "INTEGER", nullable: false),
                    SkippedByJobId = table.Column<Guid>(type: "TEXT", nullable: true),
                    FaultReference = table.Column<string>(type: "TEXT", nullable: true),
                    TickerId = table.Column<Guid>(type: "TEXT", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncJobs", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "SyncJobInstances",
                columns: table => new
                {
                    JobId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Ordinal = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    ResultJson = table.Column<string>(type: "TEXT", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncJobInstances", x => new { x.JobId, x.Ordinal });
                    table.ForeignKey(
                        name: "FK_SyncJobInstances_SyncJobs_JobId",
                        column: x => x.JobId,
                        principalTable: "SyncJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateIndex(
                name: "IX_SyncJobs_CreatedAt",
                table: "SyncJobs",
                column: "CreatedAt"
            );

            migrationBuilder.CreateIndex(
                name: "IX_SyncJobs_Status",
                table: "SyncJobs",
                column: "Status"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "SyncJobInstances");

            migrationBuilder.DropTable(name: "SyncJobs");
        }
    }
}
