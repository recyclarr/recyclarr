using FastEndpoints;
using Recyclarr.Pipelines.CustomFormat;

namespace Recyclarr.Server.Features.Guide.CustomFormats;

internal sealed class Endpoint(CategorizedCustomFormatProvider customFormats)
    : Endpoint<GuideRequest, ListGuideCustomFormatsResponse>
{
    public override void Configure()
    {
        Get("/guide/{service}/custom-formats");
        Version(1);

        // FastEndpoints secures endpoints by default. No authentication scheme exists yet, so the
        // opt-out is explicit until API key auth lands (REC-153).
        AllowAnonymous();

        Description(b => b.Produces<ListGuideCustomFormatsResponse>().WithTags("Guide"));
    }

    public override async Task HandleAsync(GuideRequest req, CancellationToken ct)
    {
        var items = customFormats
            .Get(req.Service)
            .Select(x => new GuideCustomFormatSummaryResponse(
                x.Resource.TrashId,
                x.Resource.Name,
                x.Category
            ))
            .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        await Send.OkAsync(new ListGuideCustomFormatsResponse(items), ct);
    }
}
