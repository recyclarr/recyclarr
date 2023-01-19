using Autofac;

namespace Recyclarr.TrashLib.Repo;

public class RepoAutofacModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        base.Load(builder);
        builder.RegisterType<RepoUpdater>().As<IRepoUpdater>();
        builder.RegisterType<RepoMetadataParser>().As<IRepoMetadataParser>();
        builder.RegisterType<RepoMetadataBuilder>().As<IRepoMetadataBuilder>().InstancePerLifetimeScope();
    }
}
