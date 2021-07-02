using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fluxor;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Recyclarr.Code.Radarr;
using Recyclarr.Code.Radarr.Fluxor;
using Recyclarr.Components;
using TrashLib.Radarr.Config;
using TrashLib.Radarr.CustomFormat.Models;

namespace Recyclarr.Pages.Radarr.CustomFormats
{
    [UsedImplicitly]
    public sealed partial class CustomFormatsPage : IDisposable
    {
        private CustomFormatChooser? _cfChooser;
        private HashSet<SelectableCustomFormat> _currentSelection = new();
        private ServerSelector<RadarrConfig>? _serverSelector;

        [CascadingParameter]
        public CustomFormatAccessLayout? CfAccessor { get; set; }

        [Inject]
        public IDialogService DialogService { get; set; } = default!;

        [Inject]
        public IConfigRepository<RadarrConfig> ConfigRepo { get; set; } = default!;

        [Inject]
        private IState<ActiveConfig<RadarrConfig>> ActiveConfig { get; set; } = default!;

        private bool? SelectAllCheckbox { get; set; } = false;
        private List<string> ChosenCustomFormatIds => _currentSelection.Select(cf => cf.Item.TrashId).ToList();
        private bool IsRefreshDisabled => CfAccessor is not {IsLoaded: true};
        private bool IsAddSelectedDisabled => IsRefreshDisabled || _cfChooser!.SelectedCount == 0;

        private IReadOnlyCollection<ProcessedCustomFormatData> CustomFormats
            => CfAccessor?.GuideProcessor.CustomFormats ?? new List<ProcessedCustomFormatData>();

        public void Dispose()
        {
            if (CfAccessor != null)
            {
                CfAccessor.OnReload -= OnReloadCompleted;
            }
        }

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();

            if (CfAccessor != null)
            {
                CfAccessor.OnReload += OnReloadCompleted;
            }
        }

        private void OnReloadCompleted()
        {
            _cfChooser?.RequestRefreshList();
            StateHasChanged();
        }

        private void UpdateSelectedCustomFormats(RadarrConfig? selection)
        {
            if (selection == null)
            {
                return;
            }

            _currentSelection = selection.CustomFormats
                .Select(cf =>
                {
                    var exists = CfAccessor?.GuideProcessor.CustomFormats.Any(id2 => id2.TrashId == cf.TrashId);
                    return new SelectableCustomFormat(cf, exists ?? false);
                })
                .ToHashSet();

            _cfChooser?.RequestRefreshList();
        }

        private void SelectedCustomFormatsChanged()
        {
            _cfChooser?.RequestRefreshList();

            if (ActiveConfig.Value.Config != null)
            {
                var container = ActiveConfig.Value.Config.CustomFormats;
                container.Clear();
                container.AddRange(_currentSelection
                    .Where(s => s.ExistsInGuide)
                    .Select(s => s.Item));
            }

            ConfigRepo.Save();
        }

        private IEnumerable<SelectableCustomFormat> GetSelected() => _currentSelection.Where(i => i.Selected);

        private void OnDeleteSelected()
        {
            foreach (var sel in GetSelected())
            {
                _currentSelection.Remove(sel);
            }

            UpdateSelectAllCheckbox();
            SelectedCustomFormatsChanged();
        }

        private void OnSelectAllCheckboxChanged(bool? state)
        {
            SelectAllCheckbox = state;

            if (state == null)
            {
                return;
            }

            foreach (var sel in _currentSelection)
            {
                sel.Selected = state.Value;
            }
        }

        private void ToggleSelected(SelectableCustomFormat cf) => SetSelected(cf, !cf.Selected);

        private void SetSelected(SelectableCustomFormat cf, bool isChecked)
        {
            cf.Selected = isChecked;
            UpdateSelectAllCheckbox();
        }

        private void UpdateSelectAllCheckbox()
        {
            var count = GetSelected().Count();
            if (count == 0)
            {
                SelectAllCheckbox = false;
            }
            else if (count == _currentSelection.Count)
            {
                SelectAllCheckbox = true;
            }
            else
            {
                SelectAllCheckbox = null;
            }
        }

        private async void ShowModal()
        {
            var dlg = DialogService.Show<SelectCustomFormatsModal>("Select Custom Formats",
                new DialogParameters
                {
                    ["ExcludedCustomFormatTrashIds"] = ChosenCustomFormatIds
                },
                new DialogOptions
                {
                    MaxWidth = MaxWidth.ExtraSmall,
                    FullWidth = true
                });

            var result = await dlg.Result;
            if (!result.Cancelled)
            {
                var selections = (IEnumerable<ProcessedCustomFormatData>) result.Data;
                MergeChosenCustomFormats(selections);
            }

            StateHasChanged();
        }

        private void MergeChosenCustomFormats(IEnumerable<ProcessedCustomFormatData> selections)
        {
            _currentSelection.UnionWith(selections
                .Select(cf => new SelectableCustomFormat(CreateCustomFormatConfig(cf), true)));
            SelectedCustomFormatsChanged();
        }

        private static CustomFormatConfig CreateCustomFormatConfig(ProcessedCustomFormatData cf)
        {
            return new() {TrashId = cf.TrashId, Name = cf.Name};
        }

        private void OnChooserAddSelected()
        {
            if (_cfChooser != null)
            {
                MergeChosenCustomFormats(_cfChooser.Selected);
            }
        }

        private void ForceReload()
        {
            CfAccessor?.ForceReload();
        }
    }
}
