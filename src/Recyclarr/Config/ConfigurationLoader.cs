using System.IO.Abstractions;
using FluentValidation;
using Recyclarr.Logging;
using Serilog;
using Serilog.Context;
using TrashLib.Config;
using TrashLib.Config.Services;
using TrashLib.Http;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Recyclarr.Config;

public class ConfigurationLoader<T> : IConfigurationLoader<T>
    where T : ServiceConfiguration
{
    private readonly ILogger _log;
    private readonly IDeserializer _deserializer;
    private readonly IFileSystem _fs;
    private readonly IValidator<T> _validator;

    public ConfigurationLoader(
        ILogger log,
        IFileSystem fs,
        IYamlSerializerFactory yamlFactory,
        IValidator<T> validator)
    {
        _log = log;
        _fs = fs;
        _validator = validator;
        _deserializer = yamlFactory.CreateDeserializer();
    }

    public ICollection<T> LoadMany(IEnumerable<string> configFiles, string configSection)
    {
        return configFiles.SelectMany(file => Load(file, configSection)).ToList();
    }

    public ICollection<T> Load(string file, string configSection)
    {
        _log.Debug("Loading config file: {File}", file);
        using var logScope = LogContext.PushProperty(LogProperty.Scope, _fs.Path.GetFileName(file));

        try
        {
            using var stream = _fs.File.OpenText(file);
            return LoadFromStream(stream, configSection);
        }
        catch (EmptyYamlException)
        {
            _log.Warning("Configuration file yielded no usable configuration (is it empty?)");
            return Array.Empty<T>();
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
        return Array.Empty<T>();
    }

    public ICollection<T> LoadFromStream(TextReader stream, string requestedSection)
    {
        _log.Debug("Loading config section: {Section}", requestedSection);
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

        return ParseAllSections(parser, requestedSection);
    }

    private ICollection<T> ParseAllSections(Parser parser, string requestedSection)
    {
        var configs = new List<T>();

        parser.Consume<MappingStart>();
        while (parser.TryConsume<Scalar>(out var section))
        {
            if (section.Value == requestedSection)
            {
                configs.AddRange(ParseSingleSection(parser));
            }
            else
            {
                _log.Debug("Skipping non-matching config section {Section} at line {Line}",
                    section.Value, section.Start.Line);
                parser.SkipThisAndNestedEvents();
            }
        }

        // If any config names are null, that means user specified array-style (deprecated) instances.
        if (configs.Any(x => x.Name is null))
        {
            _log.Warning(
                "Found array-style list of instances instead of named-style. " +
                "Array-style lists of Sonarr/Radarr instances are deprecated");
        }

        return configs;
    }

    private ICollection<T> ParseSingleSection(Parser parser)
    {
        var configs = new List<T>();

        switch (parser.Current)
        {
            case MappingStart:
                ParseAndAdd<MappingStart, MappingEnd>(parser, configs);
                break;

            case SequenceStart:
                ParseAndAdd<SequenceStart, SequenceEnd>(parser, configs);
                break;
        }

        return configs;
    }

    private void ParseAndAdd<TStart, TEnd>(Parser parser, ICollection<T> configs)
        where TStart : ParsingEvent
        where TEnd : ParsingEvent
    {
        parser.Consume<TStart>();
        while (!parser.TryConsume<TEnd>(out _))
        {
            var lineNumber = parser.Current?.Start.Line;

            string? instanceName = null;
            if (parser.TryConsume<Scalar>(out var key))
            {
                instanceName = key.Value;
            }

            var newConfig = _deserializer.Deserialize<T>(parser);
            newConfig.Name = instanceName;

            var result = _validator.Validate(newConfig);
            if (result is {IsValid: false})
            {
                var printableName = instanceName ?? FlurlLogging.SanitizeUrl(newConfig.BaseUrl);
                _log.Error("Validation failed for instance config {Instance} at line {Line} with errors {Errors}",
                    printableName, lineNumber, result.Errors);
                continue;
            }

            configs.Add(newConfig);
        }
    }
}
