using System.Text;
using Autofac;
using CliFx.Attributes;
using CliFx.Exceptions;
using CliFx.Infrastructure;
using Flurl.Http;
using JetBrains.Annotations;
using Recyclarr.Migration;
using Serilog;
using TrashLib.Http;
using TrashLib.Repo;
using TrashLib.Repo.VersionControl;
using YamlDotNet.Core;

namespace Recyclarr.Command;

public abstract class ServiceCommand : BaseCommand, IServiceCommand
{
    [CommandOption("preview", 'p', Description =
        "Only display the processed markdown results without making any API calls.")]
    // ReSharper disable once MemberCanBeProtected.Global
    public bool Preview { get; [UsedImplicitly] set; } = false;

    [CommandOption("config", 'c', Description =
        "One or more YAML config files to use. All configs will be used and settings are additive. " +
        "If not specified, the script will look for `recyclarr.yml` in the same directory as the executable.")]
    // ReSharper disable once MemberCanBeProtected.Global
    public IReadOnlyCollection<string> Config { get; [UsedImplicitly] set; } = new List<string>();

    [CommandOption("app-data", Description =
        "Explicitly specify the location of the recyclarr application data directory. " +
        "Mainly for usage in Docker; not recommended for normal use.")]
    public override string? AppDataDirectory { get; [UsedImplicitly] set; }

    public abstract string Name { get; }

    protected override void RegisterServices(ContainerBuilder builder)
    {
        builder.RegisterInstance(this).As<IServiceCommand>();
    }

    public sealed override async ValueTask ExecuteAsync(IConsole console)
    {
        try
        {
            await base.ExecuteAsync(console);
        }
        catch (YamlException e)
        {
            var message = e.InnerException is not null ? e.InnerException.Message : e.Message;
            var msg = new StringBuilder();
            msg.AppendLine($"Found Unrecognized YAML Property: {message}");
            msg.AppendLine("Please remove the property quoted in the above message from your YAML file");
            msg.AppendLine("Exiting due to invalid configuration");
            throw new CommandException(msg.ToString());
        }
        catch (FlurlHttpException e)
        {
            throw new CommandException(
                $"HTTP error while communicating with {Name}: {e.SanitizedExceptionMessage()}");
        }
        catch (GitCmdException e)
        {
            await console.Output.WriteLineAsync(e.ToString());
        }
        catch (Exception e) when (e is not CommandException)
        {
            throw new CommandException(e.ToString());
        }
    }

    public override async Task Process(ILifetimeScope container)
    {
        var log = container.Resolve<ILogger>();
        var repoUpdater = container.Resolve<IRepoUpdater>();
        var migration = container.Resolve<IMigrationExecutor>();

        log.Debug("Recyclarr Version: {Version}", GitVersionInformation.InformationalVersion);

        // Will throw if migration is required, otherwise just a warning is issued.
        migration.CheckNeededMigrations();

        await repoUpdater.UpdateRepo();
    }
}
