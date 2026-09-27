using System.Net;
using FastEndpoints;
using Recyclarr.Servarr.CustomFormat;
using Refit;

namespace Recyclarr.Server.Features.Instances.CustomFormats.Delete;

internal sealed class Endpoint(InstanceServiceCaller caller)
    : Endpoint<DeleteInstanceCustomFormatRequest>
{
    public override void Configure()
    {
        Delete("/instances/{name}/custom-formats/{id}");
        Version(1);

        // FastEndpoints secures endpoints by default. No authentication scheme exists yet, so the
        // opt-out is explicit until API key auth lands (REC-153).
        AllowAnonymous();

        Description(b =>
            b.ClearDefaultProduces()
                .Produces(204)
                .ProducesProblems(404)
                .Produces<ServiceFailureProblemDetails>(502, "application/problem+json")
                .WithTags("Instances")
        );
    }

    public override async Task HandleAsync(
        DeleteInstanceCustomFormatRequest req,
        CancellationToken ct
    )
    {
        var result = await caller.Call(
            req.Name,
            async (ICustomFormatService service) =>
            {
                try
                {
                    await service.DeleteCustomFormat(req.Id, ct);
                    return true;
                }
                catch (ApiException e) when (e.StatusCode == HttpStatusCode.NotFound)
                {
                    return false;
                }
            }
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

        if (!result.Value)
        {
            AddError("Custom format does not exist in the instance");
            await Send.ErrorsAsync(404, ct);
            return;
        }

        await Send.NoContentAsync(ct);
    }
}
