using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServerDatabaseMigrations
{
    /// <inheritdoc />
    public partial class RemoveSyncJobTickerId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "TickerId", table: "SyncJobs");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "TickerId",
                table: "SyncJobs",
                type: "TEXT",
                nullable: true
            );
        }
    }
}
