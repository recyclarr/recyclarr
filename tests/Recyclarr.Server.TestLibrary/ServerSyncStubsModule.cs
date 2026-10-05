using Autofac;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Recyclarr.Config.Models;
using Recyclarr.Server.Persistence;
using Recyclarr.Sync;
using Recyclarr.Sync.Results;
using Recyclarr.TestLibrary.Autofac;

namespace Recyclarr.Server.TestLibrary;

/// <summary>
/// Stubs the sync engine so server tests exercise the HTTP surface without triggering real
/// network-bound sync pipelines. Register this last so it overrides the production registrations.
/// </summary>
public sealed class ServerSyncStubsModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        // The production registration would open a file under the mock filesystem's path on the
        // real disk. Migrated here because container-only fixtures never run the server
        // bootstrap; the bootstrap's own migration is then a no-op.
        builder
            .Register(_ =>
            {
                var database = ServerDatabase.InMemory();
                using var server = DatabaseContextFactory.Server(database).CreateDbContext();
                server.Database.Migrate();
                using var queue = DatabaseContextFactory.TickerQueue(database).CreateDbContext();
                queue.Database.Migrate();
                return database;
            })
            .SingleInstance();

        builder.RegisterMockFor<ISyncOrchestrator>(m =>
            m.RunAsync(
                    Arg.Any<IReadOnlyList<IServiceConfiguration>>(),
                    Arg.Any<ISyncSettings>(),
                    Arg.Any<IInstanceSyncProgress>(),
                    Arg.Any<CancellationToken>()
                )
                .Returns(Task.FromResult(new SyncRunResult([])))
        );
    }
}
