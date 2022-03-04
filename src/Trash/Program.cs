using System.Diagnostics;
using System.Text;
using Autofac;
using CliFx;
using Trash.Command.Helpers;

namespace Trash;

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
