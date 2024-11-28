using Autofac;
using Recyclarr.Cli.Console.Commands;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Recyclarr.Cli.Console;

public static class CliSetup
{
    private static void AddCommands(IConfigurator cli)
    {
        cli.AddCommand<SyncCommand>("sync")
            .WithExample("sync", "radarr", "--instance", "movies")
            .WithExample("sync", "-i", "instance1", "-i", "instance2")
            .WithExample("sync", "sonarr", "--preview");

        cli.AddCommand<MigrateCommand>("migrate");

        cli.AddBranch(
            "list",
            list =>
            {
                list.SetDescription("List information from the guide");
                list.AddCommand<ListCustomFormatsCommand>("custom-formats");
                list.AddCommand<ListQualitiesCommand>("qualities");
                list.AddCommand<ListMediaNamingCommand>("naming");
            }
        );

        cli.AddBranch(
            "config",
            config =>
            {
                config.SetDescription("Operations for configuration files");
                config.AddCommand<ConfigCreateCommand>("create");
                config.AddBranch(
                    "list",
                    list =>
                    {
                        list.SetDescription("List configuration files in various ways");
                        list.AddCommand<ConfigListLocalCommand>("local");
                        list.AddCommand<ConfigListTemplatesCommand>("templates");
                    }
                );
            }
        );

        cli.AddBranch(
            "delete",
            delete =>
            {
                delete.SetDescription(
                    "Delete operations for remote services (e.g. Radarr, Sonarr)"
                );
                delete.AddCommand<DeleteCustomFormatsCommand>("custom-formats");
            }
        );
    }

    public static async Task<int> Run(ILifetimeScope scope, IEnumerable<string> args)
    {
        var app = scope.Resolve<CommandApp>();
        app.Configure(config =>
        {
#if DEBUG
            config.ValidateExamples();
#endif

            config.ConfigureConsole(scope.Resolve<IAnsiConsole>());
            config.PropagateExceptions();
            config.UseStrictParsing();

            config.SetApplicationName("recyclarr");
            config.SetApplicationVersion(
                $"v{GitVersionInformation.SemVer} ({GitVersionInformation.FullBuildMetaData})"
            );

            AddCommands(config);
        });

        return await app.RunAsync(args);
    }
}
