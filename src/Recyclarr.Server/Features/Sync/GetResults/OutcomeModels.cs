using System.ComponentModel;

namespace Recyclarr.Server.Features.Sync.GetResults;

internal enum OutcomeSeverity
{
    [Description("The affected item did not sync.")]
    Error,

    [Description("The item synced, but the configuration likely needs attention.")]
    Warning,

    [Description("Something notable happened that needs no action.")]
    Info,
}

/// <summary>
/// The resource an outcome is about. `trashId`, `name`, or both are present.
/// </summary>
internal sealed record OutcomeSubjectResponse
{
    /// <summary>
    /// The kind of resource, for example `customFormat`, `customFormatGroup`, or `qualityProfile`.
    /// New kinds may be added.
    /// </summary>
    public required string Kind { get; init; }

    public string? TrashId { get; init; }
    public string? Name { get; init; }
}

/// <summary>
/// One condition the sync reported, such as a configuration problem or a change the service
/// rejected. New codes may be added; a client that does not know a code can still show
/// `message`.
/// </summary>
internal sealed record OutcomeResponse
{
    /// <summary>Identifies the condition, for example `customFormatAdopted`.</summary>
    public required string Code { get; init; }

    public required OutcomeSeverity Severity { get; init; }

    /// <summary>True when the outcome stopped every pipeline of the instance.</summary>
    public required bool Blocking { get; init; }

    /// <summary>A description of the outcome for people to read.</summary>
    public required string Message { get; init; }

    /// <summary>Absent when the outcome is not about one resource.</summary>
    public OutcomeSubjectResponse? Subject { get; init; }

    /// <summary>
    /// Facts specific to the code. Absent when the code has none. The `x-outcome-data` extension
    /// of this schema maps each code to the schema of its data.
    /// </summary>
    public object? Data { get; init; }
}
