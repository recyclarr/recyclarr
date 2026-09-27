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
}

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record CreateSyncJobResponse(Guid Id, string Status, DateTimeOffset CreatedAt);

// The single 400 schema for job creation. Request validation failures fill only the standard
// fields; a selection naming unknown instances also lists the unknown and the available names.
[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed class CreateSyncJobProblemDetails : HttpValidationProblemDetails
{
    public IReadOnlyList<string>? UnknownInstances { get; init; }
    public IReadOnlyList<string>? AvailableInstances { get; init; }
}

[UsedImplicitly]
internal sealed class Validator : Validator<CreateSyncJobRequest>
{
    public Validator()
    {
        RuleFor(x => x.Service).IsInEnum();
    }
}
