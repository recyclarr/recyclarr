using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using Terminal.Gui.App;

namespace Recyclarr.Cli.Console.Wizard;

// Bridges ReactiveUI's scheduler model with Terminal.Gui's main loop.
// Adapted from Terminal.Gui's ReactiveExample.
internal sealed class TerminalScheduler(IApplication application) : LocalScheduler
{
    public override IDisposable Schedule<TState>(
        TState state,
        TimeSpan dueTime,
        Func<IScheduler, TState, IDisposable> action
    )
    {
        return dueTime == TimeSpan.Zero ? PostOnMainLoop() : PostOnMainLoopAsTimeout();

        IDisposable PostOnMainLoop()
        {
            var composite = new CompositeDisposable(2);
            var cancellation = new CancellationDisposable();

            application.Invoke(_ =>
            {
                if (!cancellation.Token.IsCancellationRequested)
                {
                    composite.Add(action(this, state));
                }
            });
            composite.Add(cancellation);

            return composite;
        }

        IDisposable PostOnMainLoopAsTimeout()
        {
            var composite = new CompositeDisposable(2);

            var timeout = application.AddTimeout(
                dueTime,
                () =>
                {
                    composite.Add(action(this, state));
                    return false;
                }
            );
            composite.Add(
                Disposable.Create(() =>
                {
                    if (timeout is not null)
                    {
                        application.RemoveTimeout(timeout);
                    }
                })
            );

            return composite;
        }
    }
}
