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
        Summary(s =>
        {
            s.Summary = "List guide custom formats";
            s.Description = "Lists the guide's custom formats for one service, sorted by name.";
            s.Responses[200] = "The custom formats.";
            s.Responses[400] = GuideRequest.InvalidServiceResponse;
        });
    }

    public override async Task HandleAsync(GuideRequest req, CancellationToken ct)
    {
        var items = customFormats
            .Get(req.Service)
            .Select(x => new GuideCustomFormatSummaryResponse
            {
                TrashId = x.Resource.TrashId,
                Name = x.Resource.Name,
                Category = x.Category,
            })
            .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        await Send.OkAsync(new ListGuideCustomFormatsResponse(items), ct);
    }
}
