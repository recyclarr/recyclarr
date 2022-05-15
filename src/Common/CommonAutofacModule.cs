using Autofac;

namespace Common;

public class CommonAutofacModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<DefaultEnvironment>().As<IEnvironment>();
        builder.RegisterType<FileUtilities>().As<IFileUtilities>();
    }
}
