using System.Diagnostics;
using System.Text;
using Autofac;
using CliFx;
using CliFx.Infrastructure;
using Recyclarr.Command.Helpers;
using Recyclarr.Migration;

namespace Recyclarr;

internal static class Program
{
    private static IContainer? _container;

    private static string ExecutableName => Process.GetCurrentProcess().ProcessName;

    public static async Task<int> Main()
    {
        _container = CompositionRoot.Setup();

        var console = _container.Resolve<IConsole>();

        try
        {
            var migration = _container.Resolve<IMigrationExecutor>();
            migration.PerformAllMigrationSteps();
        }
        catch (MigrationException e)
        {
            var msg = new StringBuilder();
            msg.AppendLine("Fatal exception during migration step. Details are below.\n");
            msg.AppendLine($"Step That Failed:  {e.OperationDescription}");
            msg.AppendLine($"Failure Reason:    {e.OriginalException.Message}");

            if (e.Remediation.Any())
            {
                msg.AppendLine("\nPossible remediation steps:");
                foreach (var remedy in e.Remediation)
                {
                    msg.AppendLine($" - {remedy}");
                }
            }

            await console.Error.WriteAsync(msg);
            return 1;
        }

        return await new CliApplicationBuilder()
            .AddCommandsFromThisAssembly()
            .SetExecutableName(ExecutableName)
            .SetVersion(BuildVersion())
            .UseTypeActivator(type => CliTypeActivator.ResolveType(_container, type))
            .UseConsole(console)
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
