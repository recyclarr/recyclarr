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
        builder.RegisterAssemblyTypes(ThisAssembly)
            .AssignableTo<IMigrationStep>()
            .As<IMigrationStep>();
    }
}
