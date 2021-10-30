using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using Serilog;

namespace Common.Extensions
{
    public static class RxExtensions
    {
        public static IObservable<T> Spy<T>(this IObservable<T> source, ILogger log, string? opName = null)
        {
            opName ??= "IObservable";
            log.Debug("{OpName}: Observable obtained on Thread: {ThreadId}",
                opName,
                Thread.CurrentThread.ManagedThreadId);

            return Observable.Create<T>(obs =>
            {
                log.Debug("{OpName}: Subscribed to on Thread: {ThreadId}",
                    opName,
                    Thread.CurrentThread.ManagedThreadId);

                try
                {
                    var subscription = source
                        .Do(
                            x => log.Debug("{OpName}: OnNext({Result}) on Thread: {ThreadId}", opName, x,
                                Thread.CurrentThread.ManagedThreadId),
                            ex => log.Debug("{OpName}: OnError({Result}) on Thread: {ThreadId}", opName, ex.Message,
                                Thread.CurrentThread.ManagedThreadId),
                            () => log.Debug("{OpName}: OnCompleted() on Thread: {ThreadId}", opName,
                                Thread.CurrentThread.ManagedThreadId))
                        .Subscribe(obs);
                    return new CompositeDisposable(
                        subscription,
                        Disposable.Create(() => log.Debug(
                            "{OpName}: Cleaned up on Thread: {ThreadId}",
                            opName,
                            Thread.CurrentThread.ManagedThreadId)));
                }
                finally
                {
                    log.Debug("{OpName}: Subscription completed", opName);
                }
            });
        }
    }
}
