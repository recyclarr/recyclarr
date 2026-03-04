using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text.Json;
using Refit;

namespace Recyclarr.Cli.ErrorHandling.Strategies;

internal class HttpExceptionStrategy : IExceptionStrategy
{
    public Task<IReadOnlyList<string>?> HandleAsync(Exception exception)
    {
        IReadOnlyList<string>? result = exception switch
        {
            ApiException e => ExtractApiErrorMessages(e),
            HttpRequestException => ["Connection failed - check your base_url"],
            _ => null,
        };

        return Task.FromResult(result);
    }

    private static List<string> ExtractApiErrorMessages(ApiException e)
    {
        var statusCode = (int)e.StatusCode;
        var statusText = $"HTTP {statusCode}";

        return e.StatusCode switch
        {
            HttpStatusCode.Unauthorized => [$"{statusText}: Unauthorized - check your api_key"],
            _ => ParseResponseBody(e.Content, statusText),
        };
    }

    private static List<string> ParseResponseBody(string? body, string statusText)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return [statusText];
        }

        var parsed = TryParseErrorMessages(body, statusText);
        return parsed.Count > 0 ? parsed : [statusText];
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types")]
    private static List<string> TryParseErrorMessages(string body, string statusText)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            // Array of validation errors: [{"errorMessage":"..."}]
            if (root.ValueKind == JsonValueKind.Array)
            {
                return ParseValidationErrorArray(root, statusText);
            }

            // Single object with "message" property: {"message":"..."}
            if (
                root.TryGetProperty("message", out var msgProp)
                && msgProp.ValueKind == JsonValueKind.String
                && msgProp.GetString() is { } msg
            )
            {
                return [$"{statusText}: {msg}"];
            }

            // ServiceErrorsList format: {"Title":"...","Errors":{"field":["msg"]}}
            return ParseServiceErrorsList(root, statusText);
        }
        catch
        {
            return [];
        }
    }

    private static List<string> ParseValidationErrorArray(JsonElement root, string statusText)
    {
        return root.EnumerateArray()
            .Where(item =>
                item.TryGetProperty("errorMessage", out var errProp)
                && errProp.ValueKind == JsonValueKind.String
            )
            .Select(item => $"{statusText}: {item.GetProperty("errorMessage").GetString()}")
            .ToList();
    }

    private static List<string> ParseServiceErrorsList(JsonElement root, string statusText)
    {
        if (
            !root.TryGetProperty("Title", out var titleProp)
            || titleProp.ValueKind != JsonValueKind.String
            || titleProp.GetString() is not { } title
        )
        {
            return [];
        }

        var messages = new List<string> { $"{statusText}: {title}" };

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
                    messages.Add($"{prop.Name}: {errText}");
                }
            }
        }

        return messages;
    }
}
