using Microsoft.EntityFrameworkCore;

namespace Recyclarr.Server.Persistence;

// Registered with Autofac rather than through AddDbContextFactory so containers built without the
// ASP.NET service collection (integration fixtures) resolve it too. TickerQ finds its context
// factory through the same container.
internal sealed class DatabaseContextFactory<TContext>(
    ServerDatabase database,
    string migrationsHistoryTable,
    Func<DbContextOptions<TContext>, TContext> create
) : IDbContextFactory<TContext>
    where TContext : DbContext
{
    private readonly DbContextOptions<TContext> _options = BuildOptions(
        database,
        migrationsHistoryTable
    );

    public TContext CreateDbContext() => create(_options);

    private static DbContextOptions<TContext> BuildOptions(
        ServerDatabase database,
        string migrationsHistoryTable
    )
    {
        var builder = new DbContextOptionsBuilder<TContext>();
        database.Configure(builder, migrationsHistoryTable);
        return builder.Options;
    }
}

internal static class DatabaseContextFactory
{
    public static DatabaseContextFactory<ServerDbContext> Server(ServerDatabase database) =>
        new(database, ServerDbContext.MigrationsHistoryTable, options => new(options));

    public static DatabaseContextFactory<TickerQueueDbContext> TickerQueue(
        ServerDatabase database
    ) => new(database, TickerQueueDbContext.MigrationsHistoryTable, options => new(options));
}
