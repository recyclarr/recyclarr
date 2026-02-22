using System.IO.Abstractions;
using Recyclarr.Config.Parsing.ErrorHandling;
using Recyclarr.Logging;
using Recyclarr.Yaml;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace Recyclarr.Config.Parsing;

[UsedImplicitly]
public class ConfigParser(IYamlSerializerFactory yamlFactory)
{
    private readonly IDeserializer _deserializer = yamlFactory.CreateDeserializer(
        YamlFileType.Config
    );

    public T? Load<T>(IFileInfo file)
        where T : class
    {
        return Load<T>(file.OpenText);
    }

    public T? Load<T>(string yaml)
        where T : class
    {
        return Load<T>(() => new StringReader(yaml));
    }

    public T? Load<T>(Func<TextReader> streamFactory)
        where T : class
    {
        try
        {
            using var stream = streamFactory();
            return _deserializer.Deserialize<T?>(stream);
        }
        catch (FeatureRemovalException e)
        {
            throw new ConfigParsingException(e.Message, 0, e);
        }
        catch (YamlException e)
        {
            // Layer 1 handlers throw ConfigParsingException inside the pipeline, which
            // YamlDotNet wraps. Unwrap and re-throw the original directly.
            if (e.FindInnerException<ConfigParsingException>() is { } inner)
            {
                throw inner;
            }

            var line = (int)e.Start.Line;
            var message = ExtractBestMessage(e, line);
            throw new ConfigParsingException(message, line, e);
        }
    }

    // Walks the exception chain to find the most specific, user-relevant message.
    // YamlDotNet often wraps the real cause in generic "Exception during deserialization" messages.
    private static string ExtractBestMessage(YamlException e, int line)
    {
        for (Exception? current = e; current is not null; current = current.InnerException)
        {
            // Skip generic wrapper messages that carry no useful information
            if (
                current.Message.Contains(
                    "Exception during deserialization",
                    StringComparison.Ordinal
                )
            )
            {
                continue;
            }

            // Skip messages that are just the type name (e.g. from reflection failures)
            if (current is MissingMethodException)
            {
                continue;
            }

            return $"YAML error at line {line}: {current.Message}";
        }

        return $"YAML error at line {line}";
    }
}
