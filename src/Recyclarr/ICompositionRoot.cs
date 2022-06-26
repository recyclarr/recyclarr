using Autofac;
using CliFx.Infrastructure;
using Serilog.Events;

namespace Recyclarr;

public interface ICompositionRoot
{
    IServiceLocatorProxy Setup(string? appDataDir, IConsole console, LogEventLevel logLevel);

    IServiceLocatorProxy Setup(ContainerBuilder builder, string? appDataDir, IConsole console,
        LogEventLevel logLevel);
}
