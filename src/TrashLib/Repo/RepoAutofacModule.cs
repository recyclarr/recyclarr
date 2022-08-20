using Autofac;

namespace TrashLib.Repo;

public class RepoAutofacModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        base.Load(builder);
        builder.RegisterType<RepoUpdater>().As<IRepoUpdater>();
        builder.RegisterType<RepoMetadataParser>().As<IRepoMetadataParser>();
        builder.RegisterType<RepoPathsFactory>().As<IRepoPathsFactory>().SingleInstance();
    }
}
