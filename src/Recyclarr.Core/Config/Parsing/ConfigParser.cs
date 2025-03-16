using System.IO.Abstractions;
using Recyclarr.Config.Parsing.ErrorHandling;
using Recyclarr.Settings;
using Recyclarr.Yaml;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace Recyclarr.Config.Parsing;

[UsedImplicitly]
public class ConfigParser(ILogger log, IYamlSerializerFactory yamlFactory)
{
    private readonly IDeserializer _deserializer = yamlFactory.CreateDeserializer();

    public T? Load<T>(IFileInfo file)
        where T : class
    {
        log.Debug("Loading config file: {File}", file);
        return Load<T>(file.OpenText);
    }

    public T? Load<T>(string yaml)
        where T : class
    {
        log.Debug("Loading config from string data");
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
            log.Error(e, "Unsupported feature");
        }
        catch (YamlException e)
        {
            var line = e.Start.Line;

            switch (e.InnerException)
            {
                case InvalidCastException:
                    log.Error(
                        e.InnerException,
                        "Incompatible value assigned/used at line {Line}",
                        line
                    );
                    break;

                default:
                    log.Error(e.InnerException, "Exception at line {Line}", line);
                    break;
            }

            var context = SettingsContextualMessages.GetContextualErrorFromException(e);
            if (context is not null)
            {
                log.Error(context);
            }
        }

        log.Error("Due to previous exception, this config will be skipped");
        return null;
    }
}
