using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Components;
using Recyclarr.Code.Radarr;

namespace Recyclarr.Pages.Radarr.CustomFormats
{
    public partial class CustomFormatChooser
    {
        private readonly Queue<Action> _performOnNextRender = new();
        private List<SelectableItem<CustomFormatIdentifier>> _selectableItems = new();

        [Parameter]
        public List<string> ExcludedCustomFormatTrashIds { get; set; } = new();

        [Parameter]
        public Action? OnListStateChanged { get; set; }

        [Parameter]
        public string Style { get; set; } = "";

        [Parameter]
        public IList<CustomFormatIdentifier>? CfIdentifiers { get; set; }

        public int SelectedCount => _selectableItems.Count(i => i.Selected);

        public IEnumerable<CustomFormatIdentifier> Selected =>
            _selectableItems.Where(i => i.Selected).Select(i => i.Item).ToList();

        protected override void OnInitialized()
        {
            RefreshSelectableItems();
        }

        protected override void OnParametersSet()
        {
            base.OnParametersSet();
            while (_performOnNextRender.TryDequeue(out var action))
            {
                action();
            }
        }

        private void ItemSelected(SelectableItem<CustomFormatIdentifier> item, bool isChecked)
        {
            item.Selected = isChecked;
            OnListStateChanged?.Invoke();
        }

        private void ItemToggled(SelectableItem<CustomFormatIdentifier> item)
        {
            ItemSelected(item, !item.Selected);
        }

        public void RequestRefreshList()
        {
            _performOnNextRender.Enqueue(RefreshSelectableItems);
        }

        private void RefreshSelectableItems()
        {
            if (CfIdentifiers == null)
            {
                return;
            }

            _selectableItems = CfIdentifiers
                .Where(cf => ExcludedCustomFormatTrashIds.All(id => cf.TrashId != id))
                .Select(cf => new SelectableItem<CustomFormatIdentifier>(cf))
                .ToList();
        }
    }
}
