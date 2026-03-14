using System.Reactive.Disposables;
using System.Reactive.Linq;
using ReactiveUI;

namespace Recyclarr.Cli.Console.Wizard.ViewModels;

// Base class for wizard step ViewModels providing IWizardStepViewModel defaults
// and a CompositeDisposable for subscription cleanup.
internal abstract class WizardStepViewModel : ReactiveObject, IWizardStepViewModel, IDisposable
{
    protected readonly CompositeDisposable Disposables = [];

    public abstract string SectionName { get; }
    public virtual IObservable<bool> IsValid => Observable.Return(true);

    public void Dispose()
    {
        Disposables.Dispose();
    }
}
