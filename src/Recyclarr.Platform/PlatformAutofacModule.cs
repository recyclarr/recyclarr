using Autofac;

namespace Recyclarr.Platform;

/// <summary>
/// Registers the process environment, the configuration directory lookup, and the server API key
/// lookup. The host registers
/// <c>System.IO.Abstractions.IFileSystem</c>.
/// </summary>
public class PlatformAutofacModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<DefaultEnvironment>().As<IEnvironment>();
        builder.RegisterType<ConfigDirectoryLocator>();
        builder.RegisterType<ServerApiKeyLocator>();
    }
}
