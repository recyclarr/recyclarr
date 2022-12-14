using Autofac;
using Recyclarr.Cli.Migration.Steps;

namespace Recyclarr.Cli.Migration;

public class MigrationAutofacModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        base.Load(builder);
        builder.RegisterType<MigrationExecutor>().As<IMigrationExecutor>();

        // Migration Steps
        builder.RegisterTypes
            (
                typeof(MigrateTrashYml),
                typeof(MigrateTrashUpdaterAppDataDir)
            )
            .As<IMigrationStep>();
    }
}
