using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using TrashLib.Config;

namespace Recyclarr.Components
{
    public partial class ServerSelector<TConfig>
        where TConfig : IServiceConfiguration
    {
        private readonly Queue<Func<Task>> _afterRenderActions = new();

        [Inject]
        public ILocalStorageService LocalStorage { get; set; } = default!;

        [Inject]
        public IConfigProvider<TConfig> ConfigProvider { get; set; } = default!;

        [Parameter]
        public string Label { get; set; } = "Select Server";

        [Parameter]
        public EventCallback<TConfig?> SelectionChanged { get; set; }

        public TConfig? Selection { get; private set; }

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();

            _afterRenderActions.Enqueue(LoadSelected);
        }

        private async Task LoadSelected()
        {
            var savedSelection = await LocalStorage.GetItemAsync<string>("selectedInstance");
            var instanceToSelect = ConfigProvider.Configs.FirstOrDefault(c => c.BaseUrl == savedSelection);
            SetSelected(instanceToSelect ?? ConfigProvider.Configs.FirstOrDefault(), false);
        }

        private async Task SaveSelected()
        {
            await LocalStorage.SetItemAsync("selectedInstance", Selection?.BaseUrl);
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            while (_afterRenderActions.TryDequeue(out var action))
            {
                await action.Invoke();
                StateHasChanged();
            }

            await base.OnAfterRenderAsync(firstRender);
        }

        private async Task OnSelectionChanged(TConfig selected)
        {
            SetSelected(selected, true);
            await SelectionChanged.InvokeAsync(Selection);
        }

        private void SetSelected(TConfig? value, bool shouldSave)
        {
            Selection = value;
            if (shouldSave)
            {
                _afterRenderActions.Enqueue(SaveSelected);
            }
        }
    }
}
