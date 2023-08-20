using System.IO.Abstractions;
using JetBrains.Annotations;
using Recyclarr.TrashLib.Config.Parsing.ErrorHandling;
using Recyclarr.TrashLib.Config.Yaml;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace Recyclarr.TrashLib.Config.Parsing;

[UsedImplicitly]
public class ConfigParser
{
    private readonly ILogger _log;
    private readonly IDeserializer _deserializer;

    public ConfigParser(ILogger log, IYamlSerializerFactory yamlFactory)
    {
        _log = log;
        _deserializer = yamlFactory.CreateDeserializer();
    }

    public RootConfigYaml? Load(IFileInfo file)
    {
        _log.Debug("Loading config file: {File}", file);
        return Load(file.OpenText);
    }

    public RootConfigYaml? Load(string yaml)
    {
        _log.Debug("Loading config from string data");
        return Load(() => new StringReader(yaml));
    }

    public RootConfigYaml? Load(Func<TextReader> streamFactory)
    {
        try
        {
            using var stream = streamFactory();
            var config = _deserializer.Deserialize<RootConfigYaml?>(stream);
            if (config.IsConfigEmpty())
            {
                _log.Warning("Configuration is empty");
            }

            return config;
        }
        catch (FeatureRemovalException e)
        {
            _log.Error(e, "Unsupported feature");
        }
        catch (YamlException e)
        {
            _log.Debug(e, "Exception while parsing config file");

            var line = e.Start.Line;
            switch (e.InnerException)
            {
                case InvalidCastException:
                    _log.Error("Incompatible value assigned/used at line {Line}: {Msg}", line,
                        e.InnerException.Message);
                    break;

                default:
                    var msg = ContextualMessages.GetContextualErrorFromException(e) ??
                        e.InnerException?.Message ?? e.Message;
                    _log.Error("Exception at line {Line}: {Msg}", line, msg);
                    break;
            }
        }

        _log.Error("Due to previous exception, this config will be skipped");
        return null;
    }
}
