using Autofac;

namespace Recyclarr.VersionControl;

public class VersionControlAutofacModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<GitRepositoryFactory>().As<IGitRepositoryFactory>();
        base.Load(builder);
    }
}
