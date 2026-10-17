using FastEndpoints;
using FluentValidation;
using Recyclarr.TrashGuide;

namespace Recyclarr.Server.Features.Sync.CreateJob;

internal sealed record CreateSyncJobRequest
{
    /// <summary>
    /// Sync only instances of this service: `radarr` or `sonarr`. When omitted, instances of
    /// both services are eligible.
    /// </summary>
    public SupportedServices? Service { get; init; }

    /// <summary>
    /// Names of the configured instances to sync; matching ignores case. When omitted or empty,
    /// every instance of the selected service syncs.
    /// </summary>
    public IReadOnlyCollection<string>? Instances { get; init; }

    /// <summary>
    /// When true, the job computes the changes without applying them to Sonarr or Radarr.
    /// </summary>
    public bool Preview { get; init; }
}

internal sealed record CreateSyncJobResponse
{
    public required Guid Id { get; init; }

    /// <summary>The job status when it was created; see the job resource for the values.</summary>
    public required string Status { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }
}

/// <summary>
/// Problem Details for a rejected sync request. A request that names unknown instances also
/// lists the unknown and the available instance names.
/// </summary>
internal sealed class CreateSyncJobProblemDetails : HttpValidationProblemDetails
{
    /// <summary>Requested instance names that no configured instance has.</summary>
    public IReadOnlyList<string>? UnknownInstances { get; init; }

    /// <summary>Names of every configured instance.</summary>
    public IReadOnlyList<string>? AvailableInstances { get; init; }
}

internal sealed class Validator : Validator<CreateSyncJobRequest>
{
    public Validator()
    {
        RuleFor(x => x.Service).IsInEnum();
    }
}
