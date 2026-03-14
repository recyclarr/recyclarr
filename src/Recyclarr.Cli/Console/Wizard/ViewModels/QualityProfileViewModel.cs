using System.Reactive.Disposables;
using System.Reactive.Linq;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using Recyclarr.ResourceProviders.Domain;

namespace Recyclarr.Cli.Console.Wizard.ViewModels;

internal partial class QualityProfileViewModel : WizardStepViewModel
{
    private readonly WizardViewModel _wizard;
    private readonly QualityProfileGroupResourceQuery _profileGroupQuery;
    private readonly QualityProfileResourceQuery _profileQuery;
    private readonly SerialDisposable _syncSubscription = new();

    // FlagSelector bitmask value from the view
    [Reactive]
    private int? _selectedFlagValue;

    // Data for the view's FlagSelector
    [Reactive]
    private IReadOnlyList<string> _labels = [];

    [Reactive]
    private IReadOnlyList<int> _values = [];

    // Parallel arrays mapping selector index -> profile data
    private List<string> _profileKeys = [];
    private List<string> _trashIds = [];

    public override string SectionName => "Quality Profile";

    public override IObservable<bool> IsValid =>
        this.WhenAnyValue(x => x.SelectedFlagValue).Select(v => v is not null and not 0);

    public QualityProfileViewModel(
        WizardViewModel wizard,
        QualityProfileGroupResourceQuery profileGroupQuery,
        QualityProfileResourceQuery profileQuery
    )
    {
        _wizard = wizard;
        _profileGroupQuery = profileGroupQuery;
        _profileQuery = profileQuery;
        Disposables.Add(_syncSubscription);
    }

    public void Activate()
    {
        PopulateSelector();
        RestoreSelections();

        // Sync decoded selections back to wizard whenever bitmask changes
        _syncSubscription.Disposable = this.WhenAnyValue(x => x.SelectedFlagValue)
            .Skip(1)
            .Subscribe(flagValue =>
            {
                _wizard.SelectedProfiles = FlagSelectorHelper.DecodeFlagValue(
                    flagValue,
                    _profileKeys.Count,
                    i => new WizardSelection(_trashIds[i], _profileKeys[i])
                );
            });
    }

    private void PopulateSelector()
    {
        var serviceType = _wizard.ServiceType;
        var categoryName = _wizard.Category.ToString();

        var groups = _profileGroupQuery.Get(serviceType);
        var matchingGroup = groups.FirstOrDefault(g =>
            string.Equals(g.Name, categoryName, StringComparison.OrdinalIgnoreCase)
        );

        if (matchingGroup is null)
        {
            _profileKeys = [];
            _trashIds = [];
            Labels = ["(No profiles available for this category)"];
            Values = [0];
            return;
        }

        var allProfiles = _profileQuery.Get(serviceType);
        var profilesByTrashId = allProfiles
            .GroupBy(p => p.TrashId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Last(), StringComparer.OrdinalIgnoreCase);

        _profileKeys = [];
        _trashIds = [];
        var labels = new List<string>();

        foreach (var (key, trashId) in matchingGroup.Profiles)
        {
            _profileKeys.Add(key);
            _trashIds.Add(trashId);

            var displayName = profilesByTrashId.TryGetValue(trashId, out var profile)
                ? profile.Name
                : key;
            labels.Add(displayName);
        }

        var values = Enumerable.Range(0, labels.Count).Select(i => 1 << i).ToList();

        Labels = labels;
        Values = values;
        SelectedFlagValue = null;
    }

    private void RestoreSelections()
    {
        var existing = _wizard.SelectedProfiles;
        if (existing.Count == 0)
        {
            return;
        }

        var flagValue = 0;
        for (var i = 0; i < _trashIds.Count; i++)
        {
            if (
                existing.Any(p =>
                    string.Equals(p.TrashId, _trashIds[i], StringComparison.OrdinalIgnoreCase)
                )
            )
            {
                flagValue |= 1 << i;
            }
        }

        if (flagValue != 0)
        {
            SelectedFlagValue = flagValue;
        }
    }
}
