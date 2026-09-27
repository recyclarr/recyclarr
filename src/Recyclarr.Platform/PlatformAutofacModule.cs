using Autofac;
using Recyclarr.Platform;

namespace Recyclarr;

/// <summary>
/// Registers the process environment and the configuration directory lookup. The host registers
/// <c>System.IO.Abstractions.IFileSystem</c>.
/// </summary>
public class PlatformAutofacModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<DefaultEnvironment>().As<IEnvironment>();
        builder.RegisterType<ConfigDirectoryLocator>();
    }
}
