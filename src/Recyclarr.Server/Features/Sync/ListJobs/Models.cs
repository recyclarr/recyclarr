using FastEndpoints;
using FluentValidation;
using Recyclarr.Server.Sync;

namespace Recyclarr.Server.Features.Sync.ListJobs;

internal sealed record ListSyncJobsRequest
{
    /// <summary>
    /// Return only jobs with this status: `Pending`, `Running`, `Succeeded`, `Partial`,
    /// `Failed`, `Interrupted`, or `Skipped` (case-insensitive). When omitted, jobs of every
    /// status are returned.
    /// </summary>
    public string? Status { get; init; }

    /// <summary>
    /// Return only jobs with this trigger: `Manual` or `Scheduled` (case-insensitive). When
    /// omitted, jobs of both triggers are returned.
    /// </summary>
    public string? Trigger { get; init; }
}

internal sealed record SyncJobSummaryResponse
{
    public required Guid Id { get; init; }

    /// <summary>The job status; see the job resource for the values.</summary>
    public required string Status { get; init; }

    /// <summary>
    /// `Manual` for a job created through the API or CLI, `Scheduled` for a job the built-in
    /// schedule created.
    /// </summary>
    public required string Trigger { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }
}

internal sealed record ListSyncJobsResponse(IReadOnlyList<SyncJobSummaryResponse> Jobs);

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
