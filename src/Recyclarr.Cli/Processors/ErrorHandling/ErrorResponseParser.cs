using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;
using Recyclarr.TrashLib.Json;

namespace Recyclarr.Cli.Processors.ErrorHandling;

public sealed class ErrorResponseParser
{
    private readonly ILogger _log;
    private readonly Func<JsonTextReader> _streamFactory;
    private readonly JsonSerializer _serializer;

    public ErrorResponseParser(ILogger log, string responseBody)
    {
        _log = log;
        _streamFactory = () => new JsonTextReader(new StringReader(responseBody));
        _serializer = JsonSerializer.Create(GlobalJsonSerializerSettings.Services);
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types")]
    public bool DeserializeList(Func<IEnumerable<dynamic>, IEnumerable<object>> expr)
    {
        try
        {
            using var stream = _streamFactory();
            var value = _serializer.Deserialize<List<dynamic>>(stream);
            if (value is null)
            {
                return false;
            }

            var parsed = expr(value);
            foreach (var s in parsed)
            {
                _log.Error("Reason: {Message:l}", (string) s);
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types")]
    public bool Deserialize(Func<dynamic, object> expr)
    {
        try
        {
            using var stream = _streamFactory();
            var value = _serializer.Deserialize<dynamic>(stream);
            if (value is null)
            {
                return false;
            }

            _log.Error("Reason: {Message:l}", (string) expr(value));
            return true;
        }
        catch
        {
            return false;
        }
    }
}
