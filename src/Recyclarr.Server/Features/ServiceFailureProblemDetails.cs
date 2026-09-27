using Recyclarr.Sync.Results;

namespace Recyclarr.Server.Features;

// 502 response for a Sonarr or Radarr call that failed outside a sync job (ADR-019). The failure
// category is the same one sync results report for the same cause.
[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed class ServiceFailureProblemDetails : HttpValidationProblemDetails
{
    public InstanceFailureCategory Failure { get; init; }

    public static IResult Create(HttpContext context, OperationalFailure failure)
    {
        var problem = ErrorResponses.Apply(
            new ServiceFailureProblemDetails
            {
                Title = "The Sonarr or Radarr instance could not complete the request",
                Failure = failure.ToCategory(),
            },
            context,
            StatusCodes.Status502BadGateway
        );

        return TypedResults.Json(
            problem,
            statusCode: StatusCodes.Status502BadGateway,
            contentType: "application/problem+json"
        );
    }
}
