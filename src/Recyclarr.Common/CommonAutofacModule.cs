using System.Reflection;
using Autofac;
using Recyclarr.Common.FluentValidation;
using Module = Autofac.Module;

namespace Recyclarr.Common;

public class CommonAutofacModule : Module
{
    private readonly Assembly _rootAssembly;

    public CommonAutofacModule(Assembly rootAssembly)
    {
        _rootAssembly = rootAssembly;
    }

    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<DefaultEnvironment>().As<IEnvironment>();
        builder.RegisterType<FileUtilities>().As<IFileUtilities>();
        builder.RegisterType<RuntimeValidationService>().As<IRuntimeValidationService>();

        builder.Register(_ => new ResourceDataReader(_rootAssembly))
            .As<IResourceDataReader>();
    }
}
