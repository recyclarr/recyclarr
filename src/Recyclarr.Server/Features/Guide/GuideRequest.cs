using FastEndpoints;
using FluentValidation;
using Recyclarr.TrashGuide;

namespace Recyclarr.Server.Features.Guide;

// Every guide collection is scoped to one service: /guide/{service}/...
internal sealed record GuideRequest
{
    public const string InvalidServiceResponse = "The service is not `radarr` or `sonarr`.";

    /// <summary>The service whose guide data to return: `radarr` or `sonarr`.</summary>
    public SupportedServices Service { get; init; }
}

internal sealed class GuideRequestValidator : Validator<GuideRequest>
{
    public GuideRequestValidator()
    {
        RuleFor(x => x.Service).IsInEnum();
    }
}
