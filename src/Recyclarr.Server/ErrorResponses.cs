using System.Text.Json;
using FluentValidation.Results;
using Microsoft.AspNetCore.WebUtilities;

namespace Recyclarr.Server;

// Shapes every error response as RFC 9457 Problem Details. Endpoints that need extension members
// subclass HttpValidationProblemDetails and fill the common members through Apply.
internal static class ErrorResponses
{
    extension(RouteHandlerBuilder builder)
    {
        // Declares the Problem Details shape that Build produces for each status code.
        public RouteHandlerBuilder ProducesProblems(params int[] statusCodes)
        {
            foreach (var status in statusCodes)
            {
                builder.Produces<HttpValidationProblemDetails>(status, "application/problem+json");
            }

            return builder;
        }
    }

    public static HttpValidationProblemDetails Build(
        List<ValidationFailure> failures,
        HttpContext context,
        int status
    )
    {
        var errors = failures
            .GroupBy(f => JsonNamingPolicy.CamelCase.ConvertName(f.PropertyName))
            .ToDictionary(g => g.Key, g => g.Select(f => f.ErrorMessage).ToArray());

        return Apply(new HttpValidationProblemDetails(errors), context, status);
    }

    public static T Apply<T>(T problem, HttpContext context, int status)
        where T : HttpValidationProblemDetails
    {
        problem.Status = status;
        problem.Title ??= ReasonPhrases.GetReasonPhrase(status);
        problem.Instance = context.Request.Path;
        problem.Extensions["traceId"] = context.TraceIdentifier;
        return problem;
    }
}
