using FastEndpoints;
using Recyclarr.ConfigTemplates;
using Recyclarr.Server.Api;

namespace Recyclarr.Server.Features.ConfigTemplates.Get;

internal sealed class Endpoint(ConfigTemplateCatalog catalog, ConfigTemplateRenderer renderer)
    : Endpoint<GetConfigTemplateRequest>
{
    private const string YamlContentType = "application/yaml";

    public override void Configure()
    {
        Get("/config-templates/{id}");
        Version(1);

        Description(b =>
            b.Produces<string>(200, YamlContentType)
                .ProducesProblems(404)
                .WithTags("Config Templates")
        );
        Summary(s =>
        {
            s.Summary = "Get a config template";
            s.Description =
                "Returns the starter configuration as YAML text, generated from the current guide "
                + "data. Clients write it to a configuration file; the server stores nothing.";
            s.Responses[200] = "The configuration YAML.";
            s.Responses[404] = "No template has this id.";
        });
    }

    public override async Task HandleAsync(GetConfigTemplateRequest req, CancellationToken ct)
    {
        var template = catalog.Find(req.Id);
        if (template is null)
        {
            AddError("Config template not found");
            await Send.ErrorsAsync(404, ct);
            return;
        }

        await Send.StringAsync(
            renderer.Render(template),
            contentType: YamlContentType,
            cancellation: ct
        );
    }
}
