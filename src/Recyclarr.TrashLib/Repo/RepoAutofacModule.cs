using Autofac;

namespace Recyclarr.TrashLib.Repo;

public class RepoAutofacModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        base.Load(builder);

        builder.RegisterType<ConfigTemplatesRepo>().As<IConfigTemplatesRepo>();
        builder.RegisterType<TrashGuidesRepo>().As<ITrashGuidesRepo>();
        builder.RegisterType<RepoUpdater>().As<IRepoUpdater>();
        builder.RegisterType<RepoMetadataBuilder>().As<IRepoMetadataBuilder>().InstancePerLifetimeScope();
    }
}
