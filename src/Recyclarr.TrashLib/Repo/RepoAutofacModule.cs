using Autofac;

namespace Recyclarr.TrashLib.Repo;

public class RepoAutofacModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        base.Load(builder);

        // Unique Repo Registrations
        builder.RegisterType<ConfigTemplatesRepo>().As<IConfigTemplatesRepo>().As<IUpdateableRepo>();
        builder.RegisterType<TrashGuidesRepo>().As<ITrashGuidesRepo>().As<IUpdateableRepo>();

        builder.RegisterType<RepoUpdater>().As<IRepoUpdater>();
        builder.RegisterType<ConsoleMultiRepoUpdater>().As<IMultiRepoUpdater>();
        builder.RegisterType<TrashRepoMetadataBuilder>().As<IRepoMetadataBuilder>().InstancePerLifetimeScope();
    }
}
