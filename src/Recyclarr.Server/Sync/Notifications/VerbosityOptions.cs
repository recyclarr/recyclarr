using Recyclarr.Settings.Models;

namespace Recyclarr.Server.Sync.Notifications;

internal sealed record VerbosityOptions(bool SendInfo, bool SendEmpty, bool SendItemDetails)
{
    public static VerbosityOptions From(NotificationVerbosity verbosity) =>
        verbosity switch
        {
            NotificationVerbosity.Minimal => new VerbosityOptions(
                SendInfo: false,
                SendEmpty: false,
                SendItemDetails: false
            ),
            NotificationVerbosity.Normal => new VerbosityOptions(
                SendInfo: true,
                SendEmpty: false,
                SendItemDetails: false
            ),
            NotificationVerbosity.Detailed => new VerbosityOptions(
                SendInfo: true,
                SendEmpty: true,
                SendItemDetails: false
            ),
            NotificationVerbosity.Verbose => new VerbosityOptions(
                SendInfo: true,
                SendEmpty: true,
                SendItemDetails: true
            ),
            _ => new VerbosityOptions(SendInfo: true, SendEmpty: false, SendItemDetails: false),
        };
}
