using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServerDatabaseMigrations
{
    /// <summary>
    /// Deletes jobs with stored results. Earlier pre-release builds stored results in a shape the
    /// current results DTO cannot read, and the server API was never released, so the history is
    /// discarded instead of converted.
    /// </summary>
    public partial class ClearPreReleaseSyncResults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "DELETE FROM SyncJobs WHERE Id IN "
                    + "(SELECT JobId FROM SyncJobInstances WHERE ResultJson IS NOT NULL);"
            );

            // Covers connections that do not enforce the cascading foreign key
            migrationBuilder.Sql(
                "DELETE FROM SyncJobInstances WHERE JobId NOT IN (SELECT Id FROM SyncJobs);"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) { }
    }
}
