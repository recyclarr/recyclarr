using Autofac;
using Autofac.Extras.Ordering;
using Recyclarr.Cli.Migration.Steps;

namespace Recyclarr.Cli.Migration;

public class MigrationAutofacModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        base.Load(builder);
        builder.RegisterType<MigrationExecutor>().As<IMigrationExecutor>();

        // Migration Steps
        builder.RegisterTypes(
                typeof(MoveOsxAppDataDotnet8),
                typeof(DeleteRepoDirMigrationStep))
            .As<IMigrationStep>()
            .OrderByRegistration();
    }
}
