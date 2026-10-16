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

        Description(b =>
            b.Produces<ListInstanceCustomFormatsResponse>()
                .ProducesProblems(404)
                .Produces<ServiceFailureProblemDetails>(502, "application/problem+json")
                .WithTags("Instances")
        );
        Summary(s =>
        {
            s.Summary = "List instance custom formats";
            s.Description =
                "Lists every custom format that exists in a configured Sonarr or Radarr "
                + "instance, including formats that Recyclarr does not manage. The server "
                + "reads them from the instance on each request. The server reads instance "
                + "configuration at startup, so an instance added later is unknown until a "
                + "restart.";
            s.Responses[200] = "The custom formats in the instance.";
            s.Responses[404] = "No configured instance has this name.";
            s.Responses[502] = ServiceFailureProblemDetails.ResponseDescription;
        });
    }

    public override async Task HandleAsync(
        ListInstanceCustomFormatsRequest req,
        CancellationToken ct
    )
    {
        var result = await caller.Call(
            req.Name,
            (ICustomFormatService service) => service.GetCustomFormats(ct),
            ct
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
            .Select(cf => new InstanceCustomFormatSummaryResponse { Id = cf.Id, Name = cf.Name })
            .ToList();
        await Send.OkAsync(new ListInstanceCustomFormatsResponse(items), ct);
    }
}
