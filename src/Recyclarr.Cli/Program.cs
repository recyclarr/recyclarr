using System.Diagnostics;
using System.Text;
using Autofac;
using CliFx;

namespace Recyclarr.Cli;

internal static class Program
{
    private static string ExecutableName => Process.GetCurrentProcess().ProcessName;

    public static async Task<int> Main()
    {
        var status = await new CliApplicationBuilder()
            .AddCommands(GetAllCommandTypes())
            .SetExecutableName(ExecutableName)
            .SetVersion(BuildVersion())
            .Build()
            .RunAsync();

        return status;
    }

    private static IEnumerable<Type> GetAllCommandTypes()
    {
        return typeof(Program).Assembly.GetTypes()
            .Where(x => x.IsAssignableTo<ICommand>() && !x.IsAbstract);
    }

    private static string BuildVersion()
    {
        var builder = new StringBuilder($"v{GitVersionInformation.MajorMinorPatch}");
        var metadata = GitVersionInformation.FullBuildMetaData;
        if (!string.IsNullOrEmpty(metadata))
        {
            builder.Append($" ({metadata})");
        }

        return builder.ToString();
    }
}
