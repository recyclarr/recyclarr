using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using Recyclarr.Json;

namespace Recyclarr.Cli.Processors.ErrorHandling;

public sealed class ErrorResponseParser(ILogger log, string responseBody)
{
    private readonly Func<Stream> _streamFactory = () =>
        new MemoryStream(Encoding.UTF8.GetBytes(responseBody));
    private readonly JsonSerializerOptions _jsonSettings = GlobalJsonSerializerSettings.Services;

    [SuppressMessage("Design", "CA1031:Do not catch general exception types")]
    public bool DeserializeList(Func<IEnumerable<JsonElement>, IEnumerable<string>> expr)
    {
        try
        {
            using var stream = _streamFactory();
            var value = JsonSerializer.Deserialize<List<JsonElement>>(stream, _jsonSettings);
            if (value is null)
            {
                return false;
            }

            var parsed = expr(value);
            foreach (var s in parsed)
            {
                log.Error("Error message from remote service: {Message:l}", s);
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types")]
    public bool Deserialize(Func<JsonElement, string?> expr)
    {
        try
        {
            using var stream = _streamFactory();
            var value = expr(JsonSerializer.Deserialize<JsonElement>(stream, _jsonSettings));
            if (value is null)
            {
                return false;
            }

            log.Error("Error message from remote service: {Message:l}", value);
            return true;
        }
        catch
        {
            return false;
        }
    }

    [UsedImplicitly(ImplicitUseKindFlags.Assign)]
    private sealed record ServiceErrorsList(string Title, Dictionary<string, List<string>> Errors);

    [SuppressMessage("Design", "CA1031:Do not catch general exception types")]
    public bool DeserializeServiceErrorList()
    {
        try
        {
            using var stream = _streamFactory();
            var value = JsonSerializer.Deserialize<ServiceErrorsList>(stream, _jsonSettings);
            if (value is null)
            {
                return false;
            }

            log.Error("Error message from remote service: {Message:l}", value.Title);

            foreach (
                var (topic, msg) in value.Errors.SelectMany(x =>
                    x.Value.Select(y => (x.Key, Msg: y))
                )
            )
            {
                log.Error("{Topic:l}: {Message:l}", topic, msg);
            }

            return true;
        }
        catch
        {
            return false;
        }
    }
}
