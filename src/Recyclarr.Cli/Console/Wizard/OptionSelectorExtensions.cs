using System.Reactive.Linq;
using Terminal.Gui.App;
using Terminal.Gui.Views;

namespace Recyclarr.Cli.Console.Wizard;

// Bridges OptionSelector<TEnum>.ValueChanged to an IObservable<TEnum?>.
// ReactiveMarbles can't generate Events() for generic types due to upstream bugs:
// - https://github.com/reactivemarbles/ObservableEvents/issues/117
// - https://github.com/reactivemarbles/ObservableEvents/issues/166
// This extension encapsulates the manual FromEventPattern boilerplate plus
// StartWith (seeds the VM with the control's default) and DistinctUntilChanged.
internal static class OptionSelectorExtensions
{
    // Observe value changes on a generic OptionSelector, seeded with the
    // control's current value so the VM receives the default immediately.
    // The optional configure lambda inserts into the pipeline before
    // StartWith + DistinctUntilChanged (e.g. for visibility filtering).
    extension<TEnum>(OptionSelector<TEnum> selector)
        where TEnum : struct, Enum
    {
        public IObservable<TEnum?> ObserveValue(
            Func<IObservable<TEnum?>, IObservable<TEnum?>>? configure = null
        )
        {
            var seed = selector.Value;
            var values = Observable
                .FromEventPattern<EventHandler<EventArgs<TEnum?>>, EventArgs<TEnum?>>(
                    h => selector.ValueChanged += h,
                    h => selector.ValueChanged -= h
                )
                .Select(e => e.EventArgs.Value);

            if (configure is not null)
            {
                values = configure(values);
            }

            return values.StartWith(seed).DistinctUntilChanged();
        }
    }
}
