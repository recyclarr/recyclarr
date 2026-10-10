using FastEndpoints;
using Recyclarr.ResourceProviders.Domain;

namespace Recyclarr.Server.Features.Guide.ScoreSets;

internal sealed class Endpoint(CustomFormatResourceQuery customFormats)
    : Endpoint<GuideRequest, ListGuideScoreSetsResponse>
{
    public override void Configure()
    {
        Get("/guide/{service}/score-sets");
        Version(1);

        // FastEndpoints secures endpoints by default. No authentication scheme exists yet, so the
        // opt-out is explicit until API key auth lands (REC-153).
        AllowAnonymous();

        Description(b => b.Produces<ListGuideScoreSetsResponse>().WithTags("Guide"));
        Summary(s =>
        {
            s.Summary = "List guide score sets";
            s.Description =
                "Lists the names of the score sets that the guide's custom formats define for "
                + "one service, sorted by name.";
            s.Responses[200] = "The score sets.";
            s.Responses[400] = GuideRequest.InvalidServiceResponse;
        });
    }

    // The guide has no score set list of its own; the sets are the score names custom formats use.
    public override async Task HandleAsync(GuideRequest req, CancellationToken ct)
    {
        var items = customFormats
            .Get(req.Service)
            .SelectMany(cf => cf.TrashScores.Keys)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .Select(name => new GuideScoreSetSummaryResponse(name))
            .ToList();

        await Send.OkAsync(new ListGuideScoreSetsResponse(items), ct);
    }
}
