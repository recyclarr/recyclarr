using Microsoft.EntityFrameworkCore;
using TickerQ.EntityFrameworkCore.Configurations;
using TickerQ.Utilities.Entities;

namespace Recyclarr.Server.Persistence;

/// <summary>
/// TickerQ's job queue tables in the server database. Only TickerQ uses this context; its schema
/// follows TickerQ releases, so a TickerQ upgrade that changes the schema needs a new migration
/// here and nowhere else.
/// </summary>
internal sealed class TickerQueueDbContext(DbContextOptions<TickerQueueDbContext> options)
    : DbContext(options)
{
    public const string MigrationsHistoryTable = "__TickerQueueMigrationsHistory";

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // SQLite has no schemas; TickerQ's default "ticker" schema would only produce warnings.
        modelBuilder.ApplyConfiguration(
            new TimeTickerConfigurations<TimeTickerEntity>(schema: null)
        );
        modelBuilder.ApplyConfiguration(
            new CronTickerConfigurations<CronTickerEntity>(schema: null)
        );
        modelBuilder.ApplyConfiguration(
            new CronTickerOccurrenceConfigurations<CronTickerEntity>(schema: null)
        );
    }
}
