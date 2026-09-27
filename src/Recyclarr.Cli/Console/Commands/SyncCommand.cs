using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Recyclarr.Cli.Console.Helpers;
using Recyclarr.Cli.Processors.Sync;
using Recyclarr.Cli.Server;
using Recyclarr.Client.V1;
using Spectre.Console.Cli;

namespace Recyclarr.Cli.Console.Commands;

[Description("Sync the guide to services")]
[UsedImplicitly]
internal class SyncCommand(ServerConnectionFactory connections, SyncCommandHandler handler)
    : AsyncCommand<SyncCommand.CliSettings>
{
    [UsedImplicitly]
    [SuppressMessage(
        "Performance",
        "CA1819:Properties should not return arrays",
        Justification = "Spectre.Console requires it"
    )]
    internal class CliSettings : BaseCommandSettings
    {
        [CommandArgument(0, "[service]")]
        [EnumDescription<SupportedServices>(
            "The service to sync. If not specified, all services are synced."
        )]
        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public SupportedServices? Service { get; init; }

        [CommandOption("-p|--preview")]
        [Description("Perform a dry run: preview the results without syncing.")]
        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public bool Preview { get; init; }

        [CommandOption("-i|--instance")]
        [Description(
            "One or more instance names to sync. If not specified, all instances will be synced."
        )]
        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public string[] Instances { get; init; } = [];
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context,
        CliSettings settings,
        CancellationToken ct
    )
    {
        // Resource providers are initialized by the server as part of its startup, so there is
        // nothing to prepare here. Disposing the connection stops an ephemeral server.
        await using var connection = await connections.ConnectAsync(ct);
        var request = new CreateSyncJobRequest
        {
            Service = settings.Service,
            Instances = [.. settings.Instances],
            Preview = settings.Preview,
        };
        return (int)await handler.RunAsync(connection.Sync, request, ct);
    }
}
