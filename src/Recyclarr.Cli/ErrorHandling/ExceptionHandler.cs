using System.Net;
using Autofac.Core;
using Recyclarr.Cli.Server;
using Recyclarr.Client.V1;
using Refit;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Recyclarr.Cli.ErrorHandling;

// Turns expected failures (server start, server HTTP errors, unreachable server, bad command
// input) into user-facing messages. Unexpected exceptions are left to the caller.
internal class ExceptionHandler(IAnsiConsole console, ILogger log)
{
    public async Task<bool> TryHandleAsync(Exception exception)
    {
        var actual = exception is DependencyResolutionException { InnerException: { } inner }
            ? inner
            : exception;

        IReadOnlyList<string>? messages = actual switch
        {
            ServerStartException e => [e.Message, .. e.Errors],
            ApiException e => await DescribeAsync(e),
            HttpRequestException e => [$"Unable to reach the Recyclarr server: {e.Message}"],
            CommandRuntimeException => [actual.Message],
            _ => null,
        };

        if (messages is null)
        {
            return false;
        }

        foreach (var message in messages)
        {
            console.MarkupLine($"[red]Error:[/] {Markup.Escape(message)}");
            log.Error("{Message}", message);
        }

        log.Error(actual, "Exiting due to fatal error");
        return true;
    }

    private static async Task<IReadOnlyList<string>> DescribeAsync(ApiException e)
    {
        if (e.StatusCode == HttpStatusCode.BadGateway)
        {
            var failure = await e.GetContentAsAsync<ServiceFailureProblemDetails>();
            return [DescribeServiceFailure(failure?.Failure)];
        }

        var problem = await e.GetContentAsAsync<HttpValidationProblemDetails>();
        var details = problem?.Errors?.Values.SelectMany(x => x) ?? [];
        return [problem?.Title ?? e.Message, .. details];
    }

    private static string DescribeServiceFailure(InstanceFailureCategory? failure) =>
        failure switch
        {
            InstanceFailureCategory.ServiceUnauthenticated =>
                "The service rejected the API key (401). Check api_key in your configuration.",
            InstanceFailureCategory.ServiceUnauthorized =>
                "The service denied access (403). Check the API key's permissions.",
            InstanceFailureCategory.ServiceRateLimited =>
                "The service is rate limiting requests (429). Try again later.",
            InstanceFailureCategory.ServiceIncompatible =>
                "The service version is not supported by Recyclarr.",
            _ => "The service could not be reached. Check base_url and that the service is up.",
        };
}
