using Autofac;

namespace Recyclarr.TrashLib.Repo.VersionControl;

public class VersionControlAutofacModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<GitRepositoryFactory>().As<IGitRepositoryFactory>();
        builder.RegisterType<GitPath>().As<IGitPath>();
        base.Load(builder);
    }
}
