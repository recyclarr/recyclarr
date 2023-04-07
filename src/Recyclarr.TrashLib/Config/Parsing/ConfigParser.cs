using System.IO.Abstractions;
using JetBrains.Annotations;
using Serilog.Context;
using YamlDotNet.Core;

namespace Recyclarr.TrashLib.Config.Parsing;

[UsedImplicitly]
public class ConfigParser
{
    private readonly ILogger _log;
    private readonly BackwardCompatibleConfigParser _parser;

    public ConfigParser(ILogger log, BackwardCompatibleConfigParser parser)
    {
        _log = log;
        _parser = parser;
    }

    public RootConfigYamlLatest? Load(IFileInfo file)
    {
        _log.Debug("Loading config file: {File}", file);
        using var logScope = LogContext.PushProperty(LogProperty.Scope, file.Name);
        return Load(file.OpenText);
    }

    public RootConfigYamlLatest? Load(string yaml)
    {
        _log.Debug("Loading config from string data");
        return Load(() => new StringReader(yaml));
    }

    public RootConfigYamlLatest? Load(Func<TextReader> streamFactory)
    {
        try
        {
            var config = _parser.ParseYamlConfig(streamFactory);
            if (config != null && config.IsConfigEmpty())
            {
                _log.Warning("Configuration is empty");
            }

            return config;
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

            _log.Error("Due to previous exception, this config will be skipped");
        }

        return null;
    }
}
