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

    public virtual bool ShouldSkip() => false;

    public virtual void Activate() { }

    // Called on submit attempt to reveal suppressed validation errors
    // (e.g. fields using ignoreInitialError that the user never touched).
    public virtual void ForceValidation() { }

    public virtual (string Title, string Message)? GetAdvanceConfirmation() => null;

    public void Dispose()
    {
        Disposables.Dispose();
    }
}
