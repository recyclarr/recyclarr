using System.Diagnostics;
using System.Text;
using Autofac;
using Autofac.Core;
using CliFx;
using CliFx.Infrastructure;
using Recyclarr.Command.Helpers;

namespace Recyclarr;

internal static class Program
{
    private static IContainer? _container;

    private static string ExecutableName => Process.GetCurrentProcess().ProcessName;

    public static async Task<int> Main()
    {
        _container = CompositionRoot.Setup();

        var console = _container.Resolve<IConsole>();

        var status = await new CliApplicationBuilder()
            .AddCommands(GetRegisteredCommandTypes())
            .SetExecutableName(ExecutableName)
            .SetVersion(BuildVersion())
            .UseTypeActivator(type => CliTypeActivator.ResolveType(_container, type))
            .UseConsole(console)
            .Build()
            .RunAsync();

        return status;
    }

    private static IEnumerable<Type> GetRegisteredCommandTypes()
    {
        if (_container is null)
        {
            throw new NullReferenceException("DI Container was null during migration process");
        }

        return _container.ComponentRegistry.Registrations
            .SelectMany(x => x.Services)
            .OfType<TypedService>()
            .Select(x => x.ServiceType)
            .Where(x => x.IsAssignableTo<ICommand>());
    }

    private static string BuildVersion()
    {
        var builder = new StringBuilder($"v{GitVersionInformation.MajorMinorPatch}");
        if (!string.IsNullOrEmpty(GitVersionInformation.BuildMetaData))
        {
            builder.Append(" (Build {GitVersionInformation.BuildMetaData})");
        }

        return builder.ToString();
    }
}
