using System.Diagnostics;
using System.Text;
using Autofac;
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
        return await new CliApplicationBuilder()
            .AddCommandsFromThisAssembly()
            .SetExecutableName(ExecutableName)
            .SetVersion(BuildVersion())
            .UseTypeActivator(type => CliTypeActivator.ResolveType(_container, type))
            .UseConsole(_container.Resolve<IConsole>())
            .Build()
            .RunAsync();
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
