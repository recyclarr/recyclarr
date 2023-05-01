using Autofac;

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
                // Add migration steps here in order of execution
            )
            .As<IMigrationStep>();
    }
}
