using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Design;

namespace Recyclarr.Server.Persistence;

// Lets `dotnet ef` build the model without starting the server host, which would resolve the real
// config directory. The in-memory database lives as long as the short-lived tool process, so it is
// never disposed.
internal sealed class DesignTimeServerDbContextFactory
    : IDesignTimeDbContextFactory<ServerDbContext>
{
    [SuppressMessage("Reliability", "CA2000", Justification = "Lives for the tool process")]
    public ServerDbContext CreateDbContext(string[] args) =>
        DatabaseContextFactory.Server(ServerDatabase.InMemory()).CreateDbContext();
}
