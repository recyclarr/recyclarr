using Autofac;
using VersionControl.Wrappers;

namespace VersionControl;

public class VersionControlAutofacModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<GitRepository>().As<IGitRepository>();
        builder.RegisterType<LibGit2SharpRepositoryStaticWrapper>().As<IRepositoryStaticWrapper>();
        builder.RegisterType<GitRepositoryFactory>().As<IGitRepositoryFactory>();
        base.Load(builder);
    }
}
