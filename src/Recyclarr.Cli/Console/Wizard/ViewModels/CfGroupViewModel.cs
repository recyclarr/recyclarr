using System.Reactive.Disposables;
using System.Reactive.Linq;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using Recyclarr.ResourceProviders.Domain;

namespace Recyclarr.Cli.Console.Wizard.ViewModels;

internal partial class CfGroupViewModel(WizardViewModel wizard, CfGroupResourceQuery cfGroupQuery)
    : WizardStepViewModel
{
    private readonly SerialDisposable _skipSync = new();
    private readonly SerialDisposable _addSync = new();

    // Skip defaults panel
    [Reactive]
    private int? _skipSelectedFlagValue;

    [Reactive]
    private IReadOnlyList<string> _skipLabels = [];

    [Reactive]
    private IReadOnlyList<int> _skipValues = [];

    private List<CfGroupResource> _skipGroups = [];

    // Add optional panel
    [Reactive]
    private int? _addSelectedFlagValue;

    [Reactive]
    private IReadOnlyList<string> _addLabels = [];

    [Reactive]
    private IReadOnlyList<int> _addValues = [];

    private List<CfGroupResource> _addGroups = [];

    public override string SectionName => "Custom Formats";

    public override bool ShouldSkip() =>
        FilterGroups(isDefault: true).Count == 0 && FilterGroups(isDefault: false).Count == 0;

    public override void Activate()
    {
        Disposables.Add(_skipSync);
        Disposables.Add(_addSync);

        // Dispose old subscriptions before populating so the null reset
        // doesn't fire stale syncs that wipe wizard state.
        _skipSync.Disposable = null;
        _addSync.Disposable = null;

        PopulatePanel(isDefault: true);
        PopulatePanel(isDefault: false);
        RestoreSelections(isDefault: true);
        RestoreSelections(isDefault: false);

        _skipSync.Disposable = this.WhenAnyValue(x => x.SkipSelectedFlagValue)
            .Skip(1)
            .Subscribe(v => wizard.SkippedCfGroups = DecodeSelections(v, _skipGroups));

        _addSync.Disposable = this.WhenAnyValue(x => x.AddSelectedFlagValue)
            .Skip(1)
            .Subscribe(v => wizard.AddedCfGroups = DecodeSelections(v, _addGroups));
    }

    private void PopulatePanel(bool isDefault)
    {
        var groups = FilterGroups(isDefault);

        if (groups.Count == 0)
        {
            if (isDefault)
            {
                _skipGroups = groups;
                SkipLabels = ["(No groups available)"];
                SkipValues = [0];
            }
            else
            {
                _addGroups = groups;
                AddLabels = ["(No groups available)"];
                AddValues = [0];
            }

            return;
        }

        var labels = groups.Select(g => g.Name).ToList();
        var values = Enumerable.Range(0, groups.Count).Select(i => 1 << i).ToList();

        if (isDefault)
        {
            _skipGroups = groups;
            SkipLabels = labels;
            SkipValues = values;
            SkipSelectedFlagValue = null;
        }
        else
        {
            _addGroups = groups;
            AddLabels = labels;
            AddValues = values;
            AddSelectedFlagValue = null;
        }
    }

    private void RestoreSelections(bool isDefault)
    {
        var groups = isDefault ? _skipGroups : _addGroups;
        var existing = isDefault ? wizard.SkippedCfGroups : wizard.AddedCfGroups;

        var flagValue = EncodeFlagValue(groups, existing);

        if (flagValue is null)
        {
            return;
        }

        if (isDefault)
        {
            SkipSelectedFlagValue = flagValue;
        }
        else
        {
            AddSelectedFlagValue = flagValue;
        }
    }

    private List<CfGroupResource> FilterGroups(bool isDefault)
    {
        var selectedProfileTrashIds = wizard
            .SelectedProfiles.Select(p => p.TrashId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var allGroups = cfGroupQuery.Get(wizard.ServiceType);
        var matchingGroups = allGroups.Where(g =>
            g.QualityProfiles.Include.Values.Any(selectedProfileTrashIds.Contains)
        );

        return matchingGroups.Where(g => IsDefault(g) == isDefault).ToList();

        static bool IsDefault(CfGroupResource g) =>
            string.Equals(g.Default, "true", StringComparison.OrdinalIgnoreCase);
    }

    private static int? EncodeFlagValue(
        List<CfGroupResource> groups,
        IReadOnlyList<WizardSelection> existing
    )
    {
        if (existing.Count == 0)
        {
            return null;
        }

        var flagValue = 0;
        for (var i = 0; i < groups.Count; i++)
        {
            if (
                existing.Any(g =>
                    string.Equals(g.TrashId, groups[i].TrashId, StringComparison.OrdinalIgnoreCase)
                )
            )
            {
                flagValue |= 1 << i;
            }
        }

        return flagValue != 0 ? flagValue : null;
    }

    private static IReadOnlyList<WizardSelection> DecodeSelections(
        int? flagValue,
        List<CfGroupResource> groups
    )
    {
        return FlagSelectorHelper.DecodeFlagValue(
            flagValue,
            groups.Count,
            i => new WizardSelection(groups[i].TrashId, groups[i].Name)
        );
    }
}
