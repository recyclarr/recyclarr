using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Recyclarr.Server.Persistence;

/// <summary>
/// Recyclarr-owned data in the server database: sync job records. Shares the database with
/// <see cref="TickerQueueDbContext"/> but not its model or migration history, so Recyclarr services
/// cannot reach TickerQ's tables and TickerQ upgrades never change these migrations.
/// </summary>
/// <remarks>
/// <para>
/// Data access through this context must stay provider-neutral (LINQ only, no raw SQL) so another
/// backend can be added later with its own migration set. Provider-specific setup belongs in
/// <see cref="ServerDatabase"/>.
/// </para>
/// <para>
/// Adding a second backend (e.g. PostgreSQL) is the trigger to move migrations out of this project.
/// EF Core keeps one migration set per provider, and a single context type with several sets
/// requires each set in its own assembly
/// (https://learn.microsoft.com/ef/core/managing-schemas/migrations/providers). At that point:
/// create one migrations project per provider (move the existing SQLite migrations and the
/// design-time factory into one), select the assembly with <c>MigrationsAssembly(...)</c> in
/// <see cref="ServerDatabase"/>, add a provider choice to <c>settings.yml</c>, and run the store
/// tests against each provider (Testcontainers for PostgreSQL). Layout reference:
/// https://learn.microsoft.com/ef/core/managing-schemas/migrations/projects
/// </para>
/// </remarks>
internal sealed class ServerDbContext(DbContextOptions<ServerDbContext> options)
    : DbContext(options)
{
    public const string MigrationsHistoryTable = "__ServerMigrationsHistory";

    public DbSet<SyncJobRecord> SyncJobs => Set<SyncJobRecord>();
    public DbSet<SyncJobInstanceRecord> SyncJobInstances => Set<SyncJobInstanceRecord>();

    // Escalates warnings that indicate a query bug: nondeterministic row limits and cartesian
    // explosion from several collection includes.
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.ConfigureWarnings(w =>
            w.Throw(
                CoreEventId.RowLimitingOperationWithoutOrderByWarning,
                CoreEventId.FirstWithoutOrderByAndFilterWarning,
                RelationalEventId.MultipleCollectionIncludeWarning
            )
        );
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder builder)
    {
        // Readable rows that survive enum member reordering.
        builder.Properties<Enum>().HaveConversion<string>();

        // SQLite cannot compare or order DateTimeOffset values stored as text.
        builder.Properties<DateTimeOffset>().HaveConversion<DateTimeOffsetToBinaryConverter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SyncJobRecord>().Navigation(x => x.Progress).AutoInclude();
    }
}
