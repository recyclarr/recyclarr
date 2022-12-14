using Autofac;
using CliFx.Attributes;
using JetBrains.Annotations;
using Recyclarr.Cli.Migration;
using Recyclarr.TrashLib.Repo;

namespace Recyclarr.Cli.Command;

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

    public override async Task Process(ILifetimeScope container)
    {
        var repoUpdater = container.Resolve<IRepoUpdater>();
        var migration = container.Resolve<IMigrationExecutor>();

        Logger.Debug("Recyclarr Version: {Version}", GitVersionInformation.InformationalVersion);

        // Will throw if migration is required, otherwise just a warning is issued.
        migration.CheckNeededMigrations();

        await repoUpdater.UpdateRepo();
    }
}
