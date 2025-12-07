using Recyclarr.Settings.Models;

namespace Recyclarr.Notifications;

public record VerbosityOptions(bool SendInfo, bool SendEmpty)
{
    public static VerbosityOptions From(NotificationVerbosity verbosity) =>
        verbosity switch
        {
            NotificationVerbosity.Minimal => new VerbosityOptions(
                SendInfo: false,
                SendEmpty: false
            ),
            NotificationVerbosity.Normal => new VerbosityOptions(SendInfo: true, SendEmpty: false),
            NotificationVerbosity.Detailed => new VerbosityOptions(SendInfo: true, SendEmpty: true),
            _ => new VerbosityOptions(SendInfo: true, SendEmpty: false),
        };
}
