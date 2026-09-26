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

[UsedImplicitly]
internal sealed class Validator : Validator<CreateSyncJobRequest>
{
    public Validator()
    {
        RuleFor(x => x.Service).IsInEnum();
    }
}
