using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Flurl.Http;

namespace Recyclarr.Cli.ErrorHandling.Strategies;

internal class HttpExceptionStrategy : IExceptionStrategy
{
    public async Task<IReadOnlyList<string>?> HandleAsync(Exception exception)
    {
        if (exception is not FlurlHttpException e)
        {
            return null;
        }

        return await ExtractErrorMessages(e);
    }

    private static async Task<IReadOnlyList<string>> ExtractErrorMessages(FlurlHttpException e)
    {
        if (e.Message.Contains("task was canceled", StringComparison.Ordinal))
        {
            return ["Operation canceled by user"];
        }

        var statusCode = e.StatusCode;
        var statusText = statusCode.HasValue ? $"HTTP {statusCode}" : "Connection error";

        return statusCode switch
        {
            401 => [$"{statusText}: Unauthorized - check your api_key"],
            null => ["Connection failed - check your base_url"],
            _ => await ParseResponseBody(e, statusText),
        };
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types")]
    private static async Task<IReadOnlyList<string>> ParseResponseBody(
        FlurlHttpException e,
        string statusText
    )
    {
        try
        {
            var body = await e.GetResponseStringAsync();
            if (string.IsNullOrWhiteSpace(body))
            {
                return [statusText];
            }

            var parsed = TryParseErrorMessages(body, statusText);
            return parsed.Count > 0 ? parsed : [statusText];
        }
        catch
        {
            return [statusText];
        }
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types")]
    private static List<string> TryParseErrorMessages(string body, string statusText)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            // Try single "message" property
            if (
                root.TryGetProperty("message", out var msgProp)
                && msgProp.ValueKind == JsonValueKind.String
                && msgProp.GetString() is { } msg
            )
            {
                return [$"{statusText}: {msg}"];
            }

            // Try array of objects with "errorMessage"
            if (root.ValueKind == JsonValueKind.Array)
            {
                var messages = new List<string> { statusText };
                foreach (var item in root.EnumerateArray())
                {
                    if (
                        item.TryGetProperty("errorMessage", out var errProp)
                        && errProp.ValueKind == JsonValueKind.String
                        && errProp.GetString() is { } errMsg
                    )
                    {
                        messages.Add(errMsg);
                    }
                }

                return messages.Count > 1 ? messages : [];
            }

            // Try ServiceErrorsList format with Title and Errors
            if (
                root.TryGetProperty("Title", out var titleProp)
                && titleProp.ValueKind == JsonValueKind.String
                && titleProp.GetString() is { } title
            )
            {
                var messages = new List<string> { $"{statusText}: {title}" };

                if (
                    root.TryGetProperty("Errors", out var errorsProp)
                    && errorsProp.ValueKind == JsonValueKind.Object
                )
                {
                    foreach (var prop in errorsProp.EnumerateObject())
                    {
                        if (prop.Value.ValueKind != JsonValueKind.Array)
                        {
                            continue;
                        }

                        foreach (var errItem in prop.Value.EnumerateArray())
                        {
                            if (
                                errItem.ValueKind == JsonValueKind.String
                                && errItem.GetString() is { } errText
                            )
                            {
                                messages.Add($"{prop.Name}: {errText}");
                            }
                        }
                    }
                }

                return messages;
            }

            return [];
        }
        catch
        {
            return [];
        }
    }
}
