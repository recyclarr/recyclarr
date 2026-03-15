using System.Reactive.Disposables;
using System.Reactive.Linq;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using Recyclarr.ResourceProviders.Domain;

namespace Recyclarr.Cli.Console.Wizard.ViewModels;

internal partial class CfGroupViewModel : WizardStepViewModel
{
    private readonly WizardViewModel _wizard;
    private readonly CfGroupResourceQuery _cfGroupQuery;
    private readonly SerialDisposable _syncSubscription = new();

    // FlagSelector bitmask value from the view
    [Reactive]
    private int? _selectedFlagValue;

    // Data for the view's FlagSelector
    [Reactive]
    private IReadOnlyList<string> _labels = [];

    [Reactive]
    private IReadOnlyList<int> _values = [];

    // Filtered groups currently shown in the selector
    private List<CfGroupResource> _groups = [];

    public CfGroupMode Mode { get; }

    public override string SectionName => "Custom Formats";

    public CfGroupViewModel(
        WizardViewModel wizard,
        CfGroupResourceQuery cfGroupQuery,
        CfGroupMode mode
    )
    {
        _wizard = wizard;
        _cfGroupQuery = cfGroupQuery;
        Mode = mode;
        Disposables.Add(_syncSubscription);
    }

    public bool ShouldSkip() => GetFilteredGroups().Count == 0;

    public override void Activate()
    {
        PopulateSelector();
        RestoreSelections();

        // Sync decoded selections back to wizard whenever bitmask changes
        _syncSubscription.Disposable = this.WhenAnyValue(x => x.SelectedFlagValue)
            .Skip(1)
            .Subscribe(flagValue =>
            {
                var selected = FlagSelectorHelper.DecodeFlagValue(
                    flagValue,
                    _groups.Count,
                    i => new WizardSelection(_groups[i].TrashId, _groups[i].Name)
                );

                if (Mode == CfGroupMode.SkipDefaults)
                {
                    _wizard.SkippedCfGroups = selected;
                }
                else
                {
                    _wizard.AddedCfGroups = selected;
                }
            });
    }

    private void PopulateSelector()
    {
        _groups = GetFilteredGroups();

        if (_groups.Count == 0)
        {
            Labels = ["(No groups available)"];
            Values = [0];
            return;
        }

        Labels = _groups.Select(g => g.Name).ToList();
        Values = Enumerable.Range(0, _groups.Count).Select(i => 1 << i).ToList();
        SelectedFlagValue = null;
    }

    private void RestoreSelections()
    {
        var existing =
            Mode == CfGroupMode.SkipDefaults ? _wizard.SkippedCfGroups : _wizard.AddedCfGroups;

        if (existing.Count == 0)
        {
            return;
        }

        var flagValue = 0;
        for (var i = 0; i < _groups.Count; i++)
        {
            if (
                existing.Any(g =>
                    string.Equals(g.TrashId, _groups[i].TrashId, StringComparison.OrdinalIgnoreCase)
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

    private List<CfGroupResource> GetFilteredGroups()
    {
        var selectedProfileTrashIds = _wizard
            .SelectedProfiles.Select(p => p.TrashId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var allGroups = _cfGroupQuery.Get(_wizard.ServiceType);
        var matchingGroups = allGroups.Where(g =>
            g.QualityProfiles.Include.Values.Any(selectedProfileTrashIds.Contains)
        );

        return matchingGroups
            .Where(g => IsDefault(g) == (Mode == CfGroupMode.SkipDefaults))
            .ToList();

        static bool IsDefault(CfGroupResource g) =>
            string.Equals(g.Default, "true", StringComparison.OrdinalIgnoreCase);
    }
}
