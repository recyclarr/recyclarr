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

[UsedImplicitly]
internal sealed class Validator : Validator<CreateSyncJobRequest>
{
    public Validator()
    {
        RuleFor(x => x.Service).IsInEnum();
    }
}
