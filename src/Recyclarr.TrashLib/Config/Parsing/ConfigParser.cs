using System.IO.Abstractions;
using FluentValidation;
using JetBrains.Annotations;
using Recyclarr.TrashLib.Config.Yaml;
using Serilog.Context;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace Recyclarr.TrashLib.Config.Parsing;

[UsedImplicitly]
public class ConfigParser
{
    private readonly ILogger _log;
    private readonly ConfigValidationExecutor _validator;
    private readonly IDeserializer _deserializer;

    public ConfigParser(
        ILogger log,
        ConfigValidationExecutor validator,
        IYamlSerializerFactory yamlFactory)
    {
        _log = log;
        _validator = validator;
        _deserializer = yamlFactory.CreateDeserializer();
    }

    public RootConfigYaml? Load(IFileInfo file)
    {
        _log.Debug("Loading config file: {File}", file);
        using var logScope = LogContext.PushProperty(LogProperty.Scope, file.Name);
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
            var config = _deserializer.Deserialize<RootConfigYaml>(stream);
            if (config.IsConfigEmpty())
            {
                _log.Warning("Configuration is empty");
            }

            if (!_validator.Validate(config))
            {
                throw new ValidationException("Validation Failed");
            }

            return config;
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
                    // Check for Configuration-specific deprecation messages
                    var msg = ConfigDeprecations.GetContextualErrorFromException(e) ??
                        e.InnerException?.Message ?? e.Message;

                    _log.Error("Exception at line {Line}: {Msg}", line, msg);
                    break;
            }

            _log.Error("Due to previous exception, this config will be skipped");
        }

        return null;
    }
}
