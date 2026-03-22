using System.Reactive.Disposables;
using System.Reactive.Linq;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using Recyclarr.TrashGuide;

namespace Recyclarr.Cli.Console.Wizard.ViewModels;

internal partial class ServiceTypeViewModel : WizardStepViewModel
{
    private readonly WizardViewModel _wizard;
    private readonly SerialDisposable _syncSubscription = new();

    [Reactive]
    private SupportedServices? _selectedServiceType;

    public override string SectionName => "Instance";

    public override IObservable<bool> IsValid =>
        this.WhenAnyValue(x => x.SelectedServiceType).Select(s => s is not null);

    public ServiceTypeViewModel(WizardViewModel wizard)
    {
        _wizard = wizard;
        Disposables.Add(_syncSubscription);
    }

    public override void Activate()
    {
        SelectedServiceType = _wizard.ServiceType;

        // Propagate selection changes to shared wizard state.
        // When service type changes, reset instance name and URL to defaults.
        _syncSubscription.Disposable = this.WhenAnyValue(x => x.SelectedServiceType)
            .Where(s => s.HasValue)
            .Select(s => s!.Value)
            .DistinctUntilChanged()
            .Subscribe(serviceType =>
            {
                _wizard.ServiceType = serviceType;
                _wizard.InstanceName = WizardViewModel.DefaultName(serviceType);
                _wizard.BaseUrl = WizardViewModel.DefaultBaseUrl(serviceType);
            });
    }
}
