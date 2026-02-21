using System.IO.Abstractions;
using Recyclarr.Config.Parsing.ErrorHandling;
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
            var line = (int)e.Start.Line;
            var context = ConfigContextualMessages.GetContextualErrorFromException(e);
            var message = e.InnerException switch
            {
                InvalidCastException => $"Incompatible value assigned/used at line {line}",
                _ => $"Exception at line {line}",
            };

            throw new ConfigParsingException(message, line, e, context);
        }
    }
}
