using System.IO.Abstractions;
using JetBrains.Annotations;
using Recyclarr.TrashLib.Config.Services;
using Recyclarr.TrashLib.Config.Services.Radarr;
using Recyclarr.TrashLib.Config.Services.Sonarr;
using Recyclarr.TrashLib.Config.Yaml;
using Serilog.Context;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Recyclarr.TrashLib.Config.Parsing;

[UsedImplicitly]
public class ConfigParser
{
    private readonly ILogger _log;
    private readonly ConfigValidationExecutor _validator;
    private readonly IDeserializer _deserializer;
    private readonly ConfigRegistry _configs = new();
    private SupportedServices? _currentSection;

    private readonly Dictionary<SupportedServices, Type> _configTypes = new()
    {
        {SupportedServices.Sonarr, typeof(SonarrConfiguration)},
        {SupportedServices.Radarr, typeof(RadarrConfiguration)}
    };

    public IConfigRegistry Configs => _configs;

    public ConfigParser(
        ILogger log,
        IYamlSerializerFactory yamlFactory,
        ConfigValidationExecutor validator)
    {
        _log = log;
        _validator = validator;
        _deserializer = yamlFactory.CreateDeserializer();
    }

    public void Load(IFileInfo file, string? desiredSection = null)
    {
        _log.Debug("Loading config file: {File}", file);
        using var logScope = LogContext.PushProperty(LogProperty.Scope, file.Name);

        try
        {
            using var stream = file.OpenText();
            LoadFromStream(stream, desiredSection);
            return;
        }
        catch (EmptyYamlException)
        {
            _log.Warning("Configuration file yielded no usable configuration (is it empty?)");
            return;
        }
        catch (YamlException e)
        {
            var line = e.Start.Line;
            switch (e.InnerException)
            {
                case InvalidCastException:
                    _log.Error("Incompatible value assigned/used at line {Line}: {Msg}", line,
                        e.InnerException.Message);
                    break;

                default:
                    _log.Error("Exception at line {Line}: {Msg}", line, e.InnerException?.Message ?? e.Message);
                    break;
            }
        }

        _log.Error("Due to previous exception, this file will be skipped: {File}", file);
    }

    public void LoadFromStream(TextReader stream, string? desiredSection)
    {
        var parser = new Parser(stream);

        parser.Consume<StreamStart>();
        if (parser.Current is StreamEnd)
        {
            _log.Debug("Skipping this config due to StreamEnd");
            throw new EmptyYamlException();
        }

        parser.Consume<DocumentStart>();
        if (parser.Current is DocumentEnd)
        {
            _log.Debug("Skipping this config due to DocumentEnd");
            throw new EmptyYamlException();
        }

        ParseAllSections(parser, desiredSection);

        if (Configs.Count == 0)
        {
            _log.Debug("Document isn't empty, but still yielded no configs");
        }
    }

    private void ParseAllSections(Parser parser, string? desiredSection)
    {
        parser.Consume<MappingStart>();
        while (parser.TryConsume<Scalar>(out var section))
        {
            if (desiredSection is not null && desiredSection != section.Value)
            {
                _log.Debug("Skipping section {Section} because it doesn't match {DesiredSection}",
                    section.Value, desiredSection);

                continue;
            }

            if (!SetCurrentSection(section.Value))
            {
                _log.Warning("Unknown service type {Type} at line {Line}; skipping",
                    section.Value, section.Start.Line);
                parser.SkipThisAndNestedEvents();
                continue;
            }

            if (!ParseSingleSection(parser))
            {
                parser.SkipThisAndNestedEvents();
            }
        }
    }

    private bool ParseSingleSection(Parser parser)
    {
        switch (parser.Current)
        {
            case MappingStart:
                ParseAndAdd<MappingStart, MappingEnd>(parser);
                break;

            case SequenceStart:
                ParseAndAdd<SequenceStart, SequenceEnd>(parser);
                break;

            case Scalar:
                _log.Debug("End of section");
                return false;

            default:
                _log.Warning("Unexpected YAML type at line {Line}; skipping this section", parser.Current?.Start.Line);
                return false;
        }

        return true;
    }

    private void ParseAndAdd<TStart, TEnd>(Parser parser)
        where TStart : ParsingEvent
        where TEnd : ParsingEvent
    {
        parser.Consume<TStart>();
        while (!parser.TryConsume<TEnd>(out _))
        {
            ParseAndAddConfig(parser);
        }
    }

    private bool SetCurrentSection(string name)
    {
        if (!Enum.TryParse(name, true, out SupportedServices key) || !_configTypes.ContainsKey(key))
        {
            return false;
        }

        _currentSection = key;
        return true;
    }

    private void ParseAndAddConfig(Parser parser)
    {
        var lineNumber = parser.Current?.Start.Line;

        string? instanceName = null;
        if (parser.TryConsume<Scalar>(out var key))
        {
            instanceName = key.Value;
        }

        if (_currentSection is null)
        {
            throw new YamlException("SetCurrentSection() must be set before parsing");
        }

        var configType = _configTypes[_currentSection.Value];
        var newConfig = (ServiceConfiguration?) _deserializer.Deserialize(parser, configType);
        if (newConfig is null)
        {
            throw new YamlException(
                $"Unable to deserialize instance at line {lineNumber} using configuration type {_currentSection}");
        }

        newConfig.InstanceName = instanceName;
        newConfig.LineNumber = lineNumber ?? 0;

        if (!_validator.Validate(newConfig))
        {
            throw new YamlException("Validation failed");
        }

        _configs.Add(newConfig);
    }
}
