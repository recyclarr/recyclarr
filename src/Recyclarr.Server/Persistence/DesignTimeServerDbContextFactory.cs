using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Design;

namespace Recyclarr.Server.Persistence;

// Let `dotnet ef` build each model without starting the server host, which would resolve the real
// config directory. Select one with `--context`. The in-memory database lives as long as the
// short-lived tool process, so it is never disposed.

[UsedImplicitly]
internal sealed class DesignTimeServerDbContextFactory
    : IDesignTimeDbContextFactory<ServerDbContext>
{
    [SuppressMessage("Reliability", "CA2000", Justification = "Lives for the tool process")]
    public ServerDbContext CreateDbContext(string[] args) =>
        DatabaseContextFactory.Server(ServerDatabase.InMemory()).CreateDbContext();
}

[UsedImplicitly]
internal sealed class DesignTimeTickerQueueDbContextFactory
    : IDesignTimeDbContextFactory<TickerQueueDbContext>
{
    [SuppressMessage("Reliability", "CA2000", Justification = "Lives for the tool process")]
    public TickerQueueDbContext CreateDbContext(string[] args) =>
        DatabaseContextFactory.TickerQueue(ServerDatabase.InMemory()).CreateDbContext();
}
