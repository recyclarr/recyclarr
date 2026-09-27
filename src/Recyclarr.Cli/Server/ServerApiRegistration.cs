using Autofac;
using Recyclarr.Cli.Settings;

namespace Recyclarr.Cli.Server;

internal static class ServerApiRegistration
{
    extension(ContainerBuilder builder)
    {
        public void RegisterServerApi()
        {
            builder.RegisterType<CliSettingsLoader>();
            builder.RegisterType<EphemeralServerLauncher>();
            builder.RegisterType<ServerConnectionFactory>();
            builder.RegisterType<HttpTrafficLogHandler>();
            builder.Register(c =>
            {
                var handler = c.Resolve<HttpTrafficLogHandler>();
                handler.InnerHandler = new SocketsHttpHandler();
                return new HttpClient(handler);
            });
        }
    }
}
