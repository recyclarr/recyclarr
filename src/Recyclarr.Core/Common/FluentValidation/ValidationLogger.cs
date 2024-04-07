using FluentValidation;
using FluentValidation.Results;
using Serilog.Events;

namespace Recyclarr.Common.FluentValidation;

public class ValidationLogger(ILogger log)
{
    private int _numErrors;

    public bool LogValidationErrors(IEnumerable<ValidationFailure> errors, string errorPrefix)
    {
        foreach (var error in errors)
        {
            var level = ToLogLevel(error.Severity);
            if (level == LogEventLevel.Error)
            {
                ++_numErrors;
            }

            log.Write(level, "{ErrorPrefix}: {Msg}", errorPrefix, error.ErrorMessage);
        }

        return _numErrors > 0;
    }

    public void LogTotalErrorCount(string errorPrefix)
    {
        if (_numErrors == 0)
        {
            return;
        }

        log.Error("{ErrorPrefix} failed with {Count} errors", errorPrefix, _numErrors);
    }

    private static LogEventLevel ToLogLevel(Severity severity)
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
