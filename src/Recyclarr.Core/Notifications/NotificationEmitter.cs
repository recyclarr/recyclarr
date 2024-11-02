using System.Reactive.Linq;
using System.Reactive.Subjects;
using Recyclarr.Notifications.Events;

namespace Recyclarr.Notifications;

public class NotificationEmitter(IVerbosityStrategy verbosity)
{
    private readonly Subject<IPresentableNotification> _notifications = new();

    public IObservable<IPresentableNotification> OnNotification => _notifications.AsObservable();

    public void SendStatistic(string description)
    {
        if (verbosity.ShouldSendInformation())
        {
            _notifications.OnNext(new InformationEvent(description));
        }
    }

    public void SendStatistic<T>(string description, T stat) where T : notnull
    {
        if (verbosity.ShouldSendInformation())
        {
            _notifications.OnNext(new InformationEvent(description)
            {
                Statistic = stat.ToString() ?? "!STAT ERROR!"
            });
        }
    }

    public void SendError(string message)
    {
        if (verbosity.ShouldSendError())
        {
            _notifications.OnNext(new ErrorEvent(message));
        }
    }

    public void SendWarning(string message)
    {
        if (verbosity.ShouldSendWarning())
        {
            _notifications.OnNext(new WarningEvent(message));
        }
    }
}
