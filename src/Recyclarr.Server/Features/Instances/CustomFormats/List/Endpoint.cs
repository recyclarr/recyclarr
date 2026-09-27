using FastEndpoints;
using Recyclarr.Servarr.CustomFormat;

namespace Recyclarr.Server.Features.Instances.CustomFormats.List;

internal sealed class Endpoint(InstanceServiceCaller caller)
    : Endpoint<ListInstanceCustomFormatsRequest, ListInstanceCustomFormatsResponse>
{
    public override void Configure()
    {
        Get("/instances/{name}/custom-formats");
        Version(1);

        // FastEndpoints secures endpoints by default. No authentication scheme exists yet, so the
        // opt-out is explicit until API key auth lands (REC-153).
        AllowAnonymous();

        Description(b =>
            b.Produces<ListInstanceCustomFormatsResponse>()
                .ProducesProblems(404)
                .Produces<ServiceFailureProblemDetails>(502, "application/problem+json")
                .WithTags("Instances")
        );
    }

    public override async Task HandleAsync(
        ListInstanceCustomFormatsRequest req,
        CancellationToken ct
    )
    {
        var result = await caller.Call(
            req.Name,
            (ICustomFormatService service) => service.GetCustomFormats(ct)
        );

        if (!result.InstanceFound)
        {
            AddError("Instance is not configured");
            await Send.ErrorsAsync(404, ct);
            return;
        }

        if (result.Failure is not null)
        {
            await Send.ResultAsync(
                ServiceFailureProblemDetails.Create(HttpContext, result.Failure)
            );
            return;
        }

        var items = (result.Value ?? [])
            .Select(cf => new InstanceCustomFormatSummaryResponse(cf.Id, cf.Name))
            .ToList();
        await Send.OkAsync(new ListInstanceCustomFormatsResponse(items), ct);
    }
}
