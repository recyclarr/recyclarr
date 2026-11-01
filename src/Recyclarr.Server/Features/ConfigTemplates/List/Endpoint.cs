using FastEndpoints;
using Recyclarr.ConfigTemplates;

namespace Recyclarr.Server.Features.ConfigTemplates.List;

internal sealed class Endpoint(ConfigTemplateCatalog catalog)
    : EndpointWithoutRequest<ListConfigTemplatesResponse>
{
    public override void Configure()
    {
        Get("/config-templates");
        Version(1);

        Description(b => b.Produces<ListConfigTemplatesResponse>().WithTags("Config Templates"));
        Summary(s =>
        {
            s.Summary = "List config templates";
            s.Description =
                "Lists the starter configurations generated from guide quality profiles, sorted by "
                + "id.";
            s.Responses[200] = "The config templates.";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var items = catalog
            .GetAll()
            .OrderBy(t => t.Id, StringComparer.Ordinal)
            .Select(t => new ConfigTemplateSummaryResponse
            {
                Id = t.Id,
                Service = t.Service,
                ProfileName = t.Profile.Name,
                ProfileTrashId = t.Profile.TrashId,
                TrashUrl = t.Profile.TrashUrl.Length == 0 ? null : t.Profile.TrashUrl,
            })
            .ToList();

        await Send.OkAsync(new ListConfigTemplatesResponse(items), ct);
    }
}
