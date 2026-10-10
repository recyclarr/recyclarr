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
                .ProducesProblems(400, 404)
                .Produces<ServiceFailureProblemDetails>(502, "application/problem+json")
                .WithTags("Instances")
        );
        Summary(s =>
        {
            s.Summary = "Delete an instance custom format";
            s.Description =
                "Deletes a custom format from a configured Sonarr or Radarr instance, whether or "
                + "not Recyclarr manages it. The server reads instance configuration at startup, "
                + "so an instance added later is unknown until a restart.";
            s.Responses[204] = "The custom format was deleted.";
            s.Responses[400] = "The ID is not an integer.";
            s.Responses[404] =
                "No configured instance has this name, or the instance has no custom format "
                + "with this ID.";
            s.Responses[502] = ServiceFailureProblemDetails.ResponseDescription;
        });
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
