using System.Reactive.Disposables;
using System.Reactive.Linq;
using Serilog;

namespace Common.Extensions;

public static class RxExtensions
{
    public static IObservable<T> Spy<T>(this IObservable<T> source, ILogger log, string? opName = null)
    {
        opName ??= "IObservable";
        log.Debug("{OpName}: Observable obtained on Thread: {ThreadId}",
            opName,
            Environment.CurrentManagedThreadId);

        return Observable.Create<T>(obs =>
        {
            log.Debug("{OpName}: Subscribed to on Thread: {ThreadId}",
                opName,
                Environment.CurrentManagedThreadId);

            try
            {
                var subscription = source
                    .Do(
                        x => log.Debug("{OpName}: OnNext({Result}) on Thread: {ThreadId}", opName, x,
                            Environment.CurrentManagedThreadId),
                        ex => log.Debug("{OpName}: OnError({Result}) on Thread: {ThreadId}", opName, ex.Message,
                            Environment.CurrentManagedThreadId),
                        () => log.Debug("{OpName}: OnCompleted() on Thread: {ThreadId}", opName,
                            Environment.CurrentManagedThreadId))
                    .Subscribe(obs);
                return new CompositeDisposable(
                    subscription,
                    Disposable.Create(() => log.Debug(
                        "{OpName}: Cleaned up on Thread: {ThreadId}",
                        opName,
                        Environment.CurrentManagedThreadId)));
            }
            finally
            {
                log.Debug("{OpName}: Subscription completed", opName);
            }
        });
    }

    // Borrowed from: https://stackoverflow.com/a/59434717/157971
    public static IObservable<T> NotNull<T>(this IObservable<T?> observable)
        where T : class
    {
        return observable.Where(x => x is not null).Select(x => x!);
    }
}
