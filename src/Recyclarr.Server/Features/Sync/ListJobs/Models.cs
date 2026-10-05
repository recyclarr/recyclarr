using FastEndpoints;
using FluentValidation;
using Recyclarr.Server.Sync;

namespace Recyclarr.Server.Features.Sync.ListJobs;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record ListSyncJobsRequest
{
    public string? Status { get; init; }
    public string? Trigger { get; init; }
}

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record SyncJobSummaryResponse(
    Guid Id,
    string Status,
    string Trigger,
    DateTimeOffset CreatedAt
);

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record ListSyncJobsResponse(IReadOnlyList<SyncJobSummaryResponse> Jobs);

[UsedImplicitly]
internal sealed class Validator : Validator<ListSyncJobsRequest>
{
    public Validator()
    {
        RuleFor(x => x.Status)
            .Must(IsNullOrName<SyncJobStatus>)
            .WithMessage(
                $"Status must be one of: {string.Join(", ", Enum.GetNames<SyncJobStatus>())}"
            );
        RuleFor(x => x.Trigger)
            .Must(IsNullOrName<SyncJobTrigger>)
            .WithMessage(
                $"Trigger must be one of: {string.Join(", ", Enum.GetNames<SyncJobTrigger>())}"
            );
    }

    // Enum.TryParse also accepts numbers, including undefined ones; filters take names only.
    private static bool IsNullOrName<T>(string? value)
        where T : struct, Enum =>
        value is null || Enum.GetNames<T>().Contains(value, StringComparer.OrdinalIgnoreCase);
}
