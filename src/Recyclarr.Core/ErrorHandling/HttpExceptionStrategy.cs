using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Refit;

namespace Recyclarr.ErrorHandling;

internal class HttpExceptionStrategy : IExceptionStrategy
{
    public Task<HandledInstanceFailure?> HandleAsync(Exception exception)
    {
        HandledInstanceFailure? result = exception switch
        {
            ApiException e => ExtractApiFailure(e),
            HttpRequestException => new HttpConnectionFailure(),
            _ => null,
        };

        return Task.FromResult(result);
    }

    private static HttpApiFailure ExtractApiFailure(ApiException e)
    {
        var statusCode = (int)e.StatusCode;
        var messages = ParseResponseBody(e.Content);
        return new HttpApiFailure(
            statusCode,
            messages,
            e.HasRequestContent,
            e.HasRequestContent ? e.RequestContent : null
        );
    }

    private static IReadOnlyList<HttpApiFailureMessage> ParseResponseBody(string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return [];
        }

        return TryParseErrorMessages(body);
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types")]
    private static IReadOnlyList<HttpApiFailureMessage> TryParseErrorMessages(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            // Array of validation errors: [{"errorMessage":"..."}]
            if (root.ValueKind == JsonValueKind.Array)
            {
                return ParseValidationErrorArray(root);
            }

            // Single object with "message" property: {"message":"..."}
            if (
                root.TryGetProperty("message", out var msgProp)
                && msgProp.ValueKind == JsonValueKind.String
                && msgProp.GetString() is { } msg
            )
            {
                return [new HttpApiResponseMessage(msg)];
            }

            // ServiceErrorsList format: {"Title":"...","Errors":{"field":["msg"]}}
            return ParseServiceErrorsList(root);
        }
        catch
        {
            return [];
        }
    }

    private static List<HttpApiResponseMessage> ParseValidationErrorArray(JsonElement root)
    {
        return root.EnumerateArray()
            .Where(item =>
                item.TryGetProperty("errorMessage", out var errProp)
                && errProp.ValueKind == JsonValueKind.String
            )
            .Select(item => new HttpApiResponseMessage(
                item.GetProperty("errorMessage").GetString() ?? ""
            ))
            .ToList();
    }

    private static List<HttpApiFailureMessage> ParseServiceErrorsList(JsonElement root)
    {
        if (
            !root.TryGetProperty("Title", out var titleProp)
            || titleProp.ValueKind != JsonValueKind.String
            || titleProp.GetString() is not { } title
        )
        {
            return [];
        }

        var messages = new List<HttpApiFailureMessage> { new HttpApiResponseMessage(title) };

        if (
            !root.TryGetProperty("Errors", out var errorsProp)
            || errorsProp.ValueKind != JsonValueKind.Object
        )
        {
            return messages;
        }

        foreach (var prop in errorsProp.EnumerateObject())
        {
            if (prop.Value.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var errItem in prop.Value.EnumerateArray())
            {
                if (errItem.ValueKind == JsonValueKind.String && errItem.GetString() is { } errText)
                {
                    messages.Add(new HttpApiFieldError(prop.Name, errText));
                }
            }
        }

        return messages;
    }
}
