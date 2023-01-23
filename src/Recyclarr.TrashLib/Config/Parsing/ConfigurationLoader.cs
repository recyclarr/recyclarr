using System.IO.Abstractions;
using Recyclarr.TrashLib.Config.Yaml;
using Serilog.Context;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;

namespace Recyclarr.TrashLib.Config.Parsing;

public class ConfigurationLoader : IConfigurationLoader
{
    private readonly ILogger _log;
    private readonly ConfigParser _parser;

    public ConfigurationLoader(ILogger log, ConfigParser parser)
    {
        _log = log;
        _parser = parser;
    }

    public IConfigRegistry LoadMany(IEnumerable<IFileInfo> configFiles, string? desiredSection = null)
    {
        foreach (var file in configFiles)
        {
            Load(file, desiredSection);
        }

        return _parser.Configs;
    }

    public IConfigRegistry Load(IFileInfo file, string? desiredSection = null)
    {
        _log.Debug("Loading config file: {File}", file);
        using var logScope = LogContext.PushProperty(LogProperty.Scope, file.Name);

        try
        {
            using var stream = file.OpenText();
            return LoadFromStream(stream, desiredSection);
        }
        catch (EmptyYamlException)
        {
            _log.Warning("Configuration file yielded no usable configuration (is it empty?)");
            return _parser.Configs;
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
        return _parser.Configs;
    }

    public IConfigRegistry LoadFromStream(TextReader stream, string? desiredSection = null)
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

        if (_parser.Configs.Count == 0)
        {
            _log.Debug("Document isn't empty, but still yielded no configs");
            throw new EmptyYamlException();
        }

        return _parser.Configs;
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

            if (!_parser.SetCurrentSection(section.Value))
            {
                _log.Warning("Unknown service type {Type} at line {Line}; skipping",
                    section.Value, section.Start.Line);
                parser.SkipThisAndNestedEvents();
                continue;
            }

            if (!ParseSingleSection(parser))
            {
                break;
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
            _parser.ParseAndAddConfig(parser);
        }
    }
}
