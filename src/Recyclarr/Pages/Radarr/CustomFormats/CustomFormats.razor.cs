using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Recyclarr.Code.Radarr;
using Recyclarr.Code.Settings.Persisters;
using Recyclarr.Components;
using TrashLib.Config;
using TrashLib.Radarr.Config;

namespace Recyclarr.Pages.Radarr.CustomFormats
{
    [UsedImplicitly]
    public partial class CustomFormats : IDisposable
    {
        private CustomFormatChooser? _cfChooser;
        private HashSet<SelectableCustomFormat> _currentSelection = new();
        private ServerSelector<RadarrConfiguration> _serverSelector = default!;

        [CascadingParameter]
        public CustomFormatAccessLayout? CfAccessor { get; set; }

        [Inject]
        public IDialogService DialogService { get; set; } = default!;

        [Inject]
        public IConfigPersister<RadarrConfiguration> ConfigPersister { get; set; } = default!;

        [Inject]
        public IConfigProvider<RadarrConfiguration> ConfigProvider { get; set; } = default!;

        private bool? SelectAllCheckbox { get; set; } = false;
        private List<string> ChosenCustomFormatIds => _currentSelection.Select(cf => cf.Item.TrashIds.First()).ToList();
        private bool IsRefreshDisabled => CfAccessor is not {IsLoaded: true};
        private bool IsAddSelectedDisabled => IsRefreshDisabled || _cfChooser!.SelectedCount == 0;

        private IList<CustomFormatIdentifier> CustomFormatIds
            => CfAccessor?.CfRepository.Identifiers ?? new List<CustomFormatIdentifier>();

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

            // _afterRenderActions.Enqueue(async () =>
            // {
            // var savedSelection = await LocalStorage.GetItemAsync<string>("selectedInstance");
            // var instanceToSelect = _configs.FirstOrDefault(c => c.BaseUrl == savedSelection);
            // _serverSelector.Selection = instanceToSelect ?? _configs.FirstOrDefault();
            // UpdateSelectedCustomFormats();
            // });

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

        private void UpdateSelectedCustomFormats(RadarrConfiguration? selection)
        {
            if (selection == null)
            {
                return;
            }

            _currentSelection = selection.CustomFormats
                .Select(cf =>
                {
                    var exists = CfAccessor?.CfRepository.Identifiers.Any(id2 => id2.TrashId == cf.TrashIds.First());
                    return new SelectableCustomFormat(cf, exists ?? false);
                })
                .ToHashSet();

            _cfChooser?.RequestRefreshList();
        }

        private void SelectedCustomFormatsChanged()
        {
            _cfChooser?.RequestRefreshList();

            if (_serverSelector.Selection != null)
            {
                _serverSelector.Selection.CustomFormats = _currentSelection
                    .Where(s => s.ExistsInGuide)
                    .Select(s => s.Item)
                    .ToList();
            }

            ConfigPersister.Save(ConfigProvider.Configs);
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
                var selections = (IEnumerable<CustomFormatIdentifier>) result.Data;
                MergeChosenCustomFormats(selections);
            }

            StateHasChanged();
        }

        private void MergeChosenCustomFormats(IEnumerable<CustomFormatIdentifier> selections)
        {
            _currentSelection.UnionWith(selections
                .Select(cf => new SelectableCustomFormat(CreateCustomFormatConfig(cf), true)));
            SelectedCustomFormatsChanged();
        }

        private static CustomFormatConfig CreateCustomFormatConfig(CustomFormatIdentifier cf)
        {
            return new()
            {
                TrashIds = new List<string> {cf.TrashId},
                Names = new List<string> {cf.Name}
            };
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
