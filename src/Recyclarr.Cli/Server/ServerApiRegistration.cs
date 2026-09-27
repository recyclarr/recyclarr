using Autofac;

namespace Recyclarr.Cli.Server;

internal static class ServerApiRegistration
{
    extension(ContainerBuilder builder)
    {
        public void RegisterServerApi()
        {
            builder.RegisterType<EphemeralServerLauncher>();
            builder.RegisterType<ServerConnectionFactory>();
            builder.RegisterType<HttpClient>();
        }
    }
}
