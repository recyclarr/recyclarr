using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Recyclarr.Server.Persistence;

namespace Recyclarr.Server.Tests.Persistence;

// Fails when an entity or model configuration changes without a matching migration, listing the
// operations the missing migration would contain. Builds contexts directly instead of through the
// server container: the container migrates its database, which throws on exactly this drift.
internal sealed class MigrationsTest
{
    [Test]
    public void Server_model_has_no_changes_without_a_migration()
    {
        using var database = ServerDatabase.InMemory();
        using var db = DatabaseContextFactory.Server(database).CreateDbContext();

        PendingOperations(db).Should().BeEmpty(Hint<ServerDbContext>());
    }

    [Test]
    public void Ticker_queue_model_has_no_changes_without_a_migration()
    {
        using var database = ServerDatabase.InMemory();
        using var db = DatabaseContextFactory.TickerQueue(database).CreateDbContext();

        PendingOperations(db).Should().BeEmpty(Hint<TickerQueueDbContext>());
    }

    private static string Hint<TContext>() =>
        $"a migration must capture the model; run: dotnet ef migrations add <Name> "
        + $"--project src/Recyclarr.Server --context {typeof(TContext).Name}";

    // Mirrors how EF's HasPendingModelChanges compares the last migration's snapshot with the
    // current model, but returns the differences instead of a bool.
    private static List<string> PendingOperations(DbContext db)
    {
        var snapshot = db.GetService<IMigrationsAssembly>().ModelSnapshot?.Model;
        var snapshotModel = snapshot is null
            ? null
            : db.GetService<IModelRuntimeInitializer>()
                .Initialize(snapshot is IMutableModel mutable ? mutable.FinalizeModel() : snapshot)
                .GetRelationalModel();
        var currentModel = db.GetService<IDesignTimeModel>().Model.GetRelationalModel();

        return db.GetService<IMigrationsModelDiffer>()
            .GetDifferences(snapshotModel, currentModel)
            .Select(Describe)
            .ToList();
    }

    private static string Describe(MigrationOperation operation) =>
        operation switch
        {
            ITableMigrationOperation table => $"{operation.GetType().Name} on {table.Table}",
            _ => operation.GetType().Name,
        };
}
