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
    }

    public override async Task HandleAsync(GuideRequest req, CancellationToken ct)
    {
        var items = qualities
            .Get(req.Service)
            .Select(q => q.Type)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .Select(type => new GuideQualitySummaryResponse(type))
            .ToList();

        await Send.OkAsync(new ListGuideQualitiesResponse(items), ct);
    }
}
