using System.Reactive.Disposables;
using System.Reactive.Linq;
using ReactiveMarbles.ObservableEvents;
using ReactiveUI;
using Terminal.Gui.Views;

namespace Recyclarr.Cli.Console.Wizard;

// Two-way binding helpers for ReactiveProperty<T> with Terminal.Gui widgets.
// ReactiveUI's built-in Bind() requires INotifyPropertyChanged on both sides,
// which Terminal.Gui controls don't implement. These extensions bridge the gap
// by wiring the VM→View and View→VM directions via observables.
internal static class ReactivePropertyBindingExtensions
{
    extension<T>(ReactiveProperty<T> property)
    {
        // Generic two-way binding between a ReactiveProperty and any view widget.
        // writeView: pushes the VM value into the widget.
        // viewChanged: observable that emits the widget's current value on user input.
        public IDisposable BindTwoWay(Action<T?> writeView, IObservable<T> viewChanged)
        {
            return new CompositeDisposable(
                property.Subscribe(writeView),
                viewChanged.DistinctUntilChanged().Subscribe(v => property.Value = v)
            );
        }
    }

    extension(ReactiveProperty<string> property)
    {
        // Convenience overload for TextField, the most common binding target.
        public IDisposable BindTwoWay(TextField field)
        {
            return property.BindTwoWay(
                v => field.Text = v ?? "",
                field.Events().TextChanged.Select(_ => field.Text)
            );
        }
    }
}
