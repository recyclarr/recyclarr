using Autofac;

namespace Recyclarr.Platform;

/// <summary>
/// Registers the special-folder lookup, the configuration directory lookup, and the server API key
/// lookup. The host registers <c>System.IO.Abstractions.IFileSystem</c> and an
/// <c>IConfiguration</c> that includes the environment (see <see cref="EnvironmentConfiguration"/>).
/// </summary>
public class PlatformAutofacModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<DefaultSpecialFolders>().As<ISpecialFolders>();
        builder.RegisterType<ConfigDirectoryLocator>();
        builder.RegisterType<ServerApiKeyLocator>();
    }
}
