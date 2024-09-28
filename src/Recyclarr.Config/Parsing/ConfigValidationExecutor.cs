using FluentValidation;
using FluentValidation.Results;
using Recyclarr.Common.FluentValidation;
using Serilog.Events;

namespace Recyclarr.Config.Parsing;

[UsedImplicitly]
public class ConfigValidationExecutor(ILogger log, IRuntimeValidationService validationService)
{
    public bool Validate(object config, params string[] ruleSets)
    {
        var result = validationService.Validate(config, ruleSets);
        if (result.IsValid)
        {
            return true;
        }

        var numErrors = result.Errors.LogValidationErrors(log, "Config Validation");
        if (numErrors == 0)
        {
            return true;
        }

        log.Error("Config validation failed with {Count} errors", numErrors);
        return false;
    }
}

public class ValidationLogger
{
    public int LogValidationErrors(IReadOnlyCollection<ValidationFailure> errors, string errorPrefix)
    {
        var numErrors = 0;

        foreach (var error in errors)
        {
            var level = ToLogLevel(error.Severity);
            if (level == LogEventLevel.Error)
            {
                ++numErrors;
            }

            log.Write(level, "{ErrorPrefix}: {Msg}", errorPrefix, error.ErrorMessage);
        }

        return numErrors;
    }

    public static LogEventLevel ToLogLevel(Severity severity)
    {
        return severity switch
        {
            Severity.Error => LogEventLevel.Error,
            Severity.Warning => LogEventLevel.Warning,
            Severity.Info => LogEventLevel.Information,
            _ => LogEventLevel.Debug
        };
    }
}
