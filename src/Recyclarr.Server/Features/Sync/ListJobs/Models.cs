using FastEndpoints;
using FluentValidation;
using Recyclarr.Server.Sync;

namespace Recyclarr.Server.Features.Sync.ListJobs;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record ListSyncJobsRequest
{
    public string? Status { get; init; }
}

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record SyncJobSummaryResponse(Guid Id, string Status, DateTimeOffset CreatedAt);

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record ListSyncJobsResponse(IReadOnlyList<SyncJobSummaryResponse> Jobs);

[UsedImplicitly]
internal sealed class Validator : Validator<ListSyncJobsRequest>
{
    public Validator()
    {
        RuleFor(x => x.Status)
            .Must(s => s is null || Enum.TryParse<SyncJobStatus>(s, ignoreCase: true, out _))
            .WithMessage("Status must be one of: Pending, Running, Succeeded, Partial, Failed");
    }
}
