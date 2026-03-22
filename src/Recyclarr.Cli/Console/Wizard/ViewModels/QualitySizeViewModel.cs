using System.Reactive.Disposables;
using System.Reactive.Linq;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace Recyclarr.Cli.Console.Wizard.ViewModels;

internal partial class QualitySizeViewModel : WizardStepViewModel
{
    private readonly WizardViewModel _wizard;
    private readonly SerialDisposable _syncSubscription = new();

    [Reactive]
    private bool? _selectedValue;

    // Display-only: the resolved quality size type shown to the user
    [Reactive]
    private string _resolvedType = "";

    public override string SectionName => "Quality Sizes";

    public override IObservable<bool> IsValid =>
        this.WhenAnyValue(x => x.SelectedValue).Select(v => v is not null);

    public QualitySizeViewModel(WizardViewModel wizard)
    {
        _wizard = wizard;
        Disposables.Add(_syncSubscription);
    }

    public override void Activate()
    {
        ResolvedType = WizardViewModel.QualitySizeType(_wizard.ServiceType, _wizard.Category);
        SelectedValue = _wizard.UseQualitySizes;

        _syncSubscription.Disposable = this.WhenAnyValue(x => x.SelectedValue)
            .Where(v => v.HasValue)
            .Select(v => v!.Value)
            .DistinctUntilChanged()
            .Subscribe(v => _wizard.UseQualitySizes = v);
    }
}
