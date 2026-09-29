using Recyclarr.Client.V1;

namespace Recyclarr.Cli.Processors.Sync;

/// <summary>
/// The status symbols shared by the sync legend, the live progress table, and the final results
/// table, so every view uses the same symbol and color for the same outcome.
/// </summary>
internal static class StatusMarkup
{
    public const string Ok = "[green]✓[/]";
    public const string Partial = "[yellow]~[/]";
    public const string Failed = "[red]✗[/]";
    public const string NotRun = "[grey]--[/]";

    public static string Of(SyncCompletionStatus status) =>
        status switch
        {
            SyncCompletionStatus.Succeeded => Ok,
            SyncCompletionStatus.Partial => Partial,
            _ => Failed,
        };

    /// <summary>
    /// Markup for a finished instance; statuses that are still in flight render as not run.
    /// </summary>
    public static string Of(InstanceProgressStatusResponse status) =>
        status switch
        {
            InstanceProgressStatusResponse.Succeeded => Ok,
            InstanceProgressStatusResponse.Partial => Partial,
            InstanceProgressStatusResponse.Failed => Failed,
            _ => NotRun,
        };
}
