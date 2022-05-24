using Recyclarr.Migration;

namespace Recyclarr.Command.Initialization.Init;

internal class CheckMigrationNeeded : IServiceInitializer
{
    private readonly IMigrationExecutor _migration;

    public CheckMigrationNeeded(IMigrationExecutor migration)
    {
        _migration = migration;
    }

    public void Initialize(ServiceCommand cmd)
    {
        _migration.CheckNeededMigrations();
    }
}
