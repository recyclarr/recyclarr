using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServerDatabaseMigrations
{
    /// <inheritdoc />
    public partial class AddSyncJobTraceParent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TraceParent",
                table: "SyncJobs",
                type: "TEXT",
                nullable: true
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "TraceParent", table: "SyncJobs");
        }
    }
}
