using FastEndpoints;
using FluentValidation;
using Recyclarr.TrashGuide;

namespace Recyclarr.Server.Features.Sync.CreateJob;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record CreateSyncJobRequest
{
    public SupportedServices? Service { get; init; }
    public IReadOnlyCollection<string>? Instances { get; init; }
    public bool Preview { get; init; }

    // Explicit config file paths, resolved on the server. Meaningful when the server shares a
    // filesystem with the caller (ephemeral launch); a remote caller has no way to name paths the
    // server can see. Empty means "use the default config locations".
    public IReadOnlyCollection<string>? Configs { get; init; }
}

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record CreateSyncJobResponse(Guid Id, string Status, DateTimeOffset CreatedAt);

// RFC 9457 Problem Details for this endpoint's 400s. One schema must cover both failure modes,
// because OpenAPI allows a single response schema per status code: request validation (populates
// Errors) and "no instances left to sync" (populates Diagnostics). Both members are therefore
// optional. FastEndpoints' own ProblemDetails is sealed, so the standard members are restated here;
// SyncJobsHttpTest guards against drift from that shape.
[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record CreateSyncJobProblemDetails
{
    public string? Type { get; init; }
    public string? Title { get; init; }
    public int Status { get; init; }
    public string? Instance { get; init; }
    public string? TraceId { get; init; }
    public string? Detail { get; init; }

    // Populated for request-validation failures; empty for config-load failures.
    public IReadOnlyCollection<ProblemErrorDetail> Errors { get; init; } = [];

    // Populated for config-load failures; null for request-validation failures.
    public ConfigDiagnosticsResponse? Diagnostics { get; init; }
}

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record ProblemErrorDetail(
    string Name,
    string Reason,
    string? Code = null,
    string? Severity = null
);

[UsedImplicitly]
internal sealed class Validator : Validator<CreateSyncJobRequest>
{
    public Validator()
    {
        RuleFor(x => x.Service).IsInEnum();
    }
}
