using FastEndpoints;
using Recyclarr.ConfigTemplates;
using Recyclarr.TrashGuide;

namespace Recyclarr.Server.Features.ConfigIncludes.List;

internal sealed class Endpoint(ConfigIncludesResourceQuery includes)
    : EndpointWithoutRequest<ListConfigIncludesResponse>
{
    public override void Configure()
    {
        Get("/config-includes");

        Description(b => b.Produces<ListConfigIncludesResponse>().WithTags("Config Templates"));
        Summary(s =>
        {
            s.Summary = "List config includes";
            s.Description =
                "Lists the include templates offered by `config-templates` resource providers, "
                + "sorted by service and id.";
            s.Responses[200] = "The config includes; empty when no provider offers any.";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var items = Enum.GetValues<SupportedServices>()
            .SelectMany(service =>
                includes
                    .Get(service)
                    .OrderBy(i => i.Id, StringComparer.Ordinal)
                    .Select(i => new ConfigIncludeSummaryResponse { Id = i.Id, Service = service })
            )
            .ToList();

        await Send.OkAsync(new ListConfigIncludesResponse(items), ct);
    }
}
