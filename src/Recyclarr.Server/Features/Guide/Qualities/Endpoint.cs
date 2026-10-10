using FastEndpoints;
using Recyclarr.ResourceProviders.Domain;

namespace Recyclarr.Server.Features.Guide.Qualities;

internal sealed class Endpoint(QualitySizeResourceQuery qualities)
    : Endpoint<GuideRequest, ListGuideQualitiesResponse>
{
    public override void Configure()
    {
        Get("/guide/{service}/qualities");
        Version(1);

        // FastEndpoints secures endpoints by default. No authentication scheme exists yet, so the
        // opt-out is explicit until API key auth lands (REC-153).
        AllowAnonymous();

        Description(b => b.Produces<ListGuideQualitiesResponse>().WithTags("Guide"));
        Summary(s =>
        {
            s.Summary = "List guide quality size types";
            s.Description =
                "Lists the names of the guide's quality size sets for one service, sorted by name.";
            s.Responses[200] = "The quality size types.";
            s.Responses[400] = GuideRequest.InvalidServiceResponse;
        });
    }

    public override async Task HandleAsync(GuideRequest req, CancellationToken ct)
    {
        var items = qualities
            .Get(req.Service)
            .Select(q => q.Type)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .Select(type => new GuideQualitySummaryResponse { Type = type })
            .ToList();

        await Send.OkAsync(new ListGuideQualitiesResponse(items), ct);
    }
}
