using FastEndpoints;
using Recyclarr.ResourceProviders.Domain;
using Recyclarr.TrashGuide;

namespace Recyclarr.Server.Features.Guide.Naming;

internal sealed class Endpoint(MediaNamingResourceQuery naming)
    : Endpoint<GuideRequest, GuideNamingResponse>
{
    public override void Configure()
    {
        Get("/guide/{service}/naming");
        Version(1);

        // FastEndpoints secures endpoints by default. No authentication scheme exists yet, so the
        // opt-out is explicit until API key auth lands (REC-153).
        AllowAnonymous();

        Description(b => b.Produces<GuideNamingResponse>().WithTags("Guide"));
        Summary(s =>
        {
            s.Summary = "Get guide naming formats";
            s.Description =
                "Returns the guide's file and folder naming formats for one service. The "
                + "`service` property tells which set of format lists the response has.";
            s.Responses[200] = "The naming formats.";
            s.Responses[400] = GuideRequest.InvalidServiceResponse;
        });
    }

    public override async Task HandleAsync(GuideRequest req, CancellationToken ct)
    {
        GuideNamingResponse response = req.Service switch
        {
            SupportedServices.Radarr => ToRadarr(naming.GetRadarr()),
            SupportedServices.Sonarr => ToSonarr(naming.GetSonarr()),
            _ => throw new ArgumentOutOfRangeException(nameof(req), req.Service, null),
        };

        await Send.OkAsync(response, ct);
    }

    private static RadarrGuideNamingResponse ToRadarr(RadarrMediaNamingResource guide) =>
        new(ToFormats(guide.Folder), ToFormats(guide.File));

    private static SonarrGuideNamingResponse ToSonarr(SonarrMediaNamingResource guide) =>
        new(
            ToFormats(guide.Season),
            ToFormats(guide.Series),
            ToFormats(guide.Episodes.Standard),
            ToFormats(guide.Episodes.Daily),
            ToFormats(guide.Episodes.Anime)
        );

    // Guide keys take the form "name" or "name:version".
    private static List<GuideNamingFormatResponse> ToFormats(
        IReadOnlyDictionary<string, string> formats
    ) =>
        formats
            .Select(x =>
            {
                var parts = x.Key.Split(':', 2);
                return new GuideNamingFormatResponse
                {
                    Key = parts[0],
                    Version = parts.Length > 1 ? parts[1] : null,
                    Format = x.Value,
                };
            })
            .ToList();
}
