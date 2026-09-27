using FastEndpoints;
using FluentValidation;
using Recyclarr.TrashGuide;

namespace Recyclarr.Server.Features.Guide;

// Every guide collection is scoped to one service: /guide/{service}/...
[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record GuideRequest
{
    public SupportedServices Service { get; init; }
}

[UsedImplicitly]
internal sealed class GuideRequestValidator : Validator<GuideRequest>
{
    public GuideRequestValidator()
    {
        RuleFor(x => x.Service).IsInEnum();
    }
}
