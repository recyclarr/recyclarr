using System.Linq;
using System.Threading.Tasks;
using Fluxor;
using Microsoft.AspNetCore.Components;
using Recyclarr.Code;
using Recyclarr.Code.Radarr;
using Recyclarr.Code.Radarr.Fluxor;
using TrashLib.Config;

namespace Recyclarr.Components
{
    public partial class ServerSelector<TConfig>
        where TConfig : IServiceConfiguration
    {
        [Inject]
        public ILocalStorageActionQueue LocalStorage { get; set; } = default!;

        [Inject]
        public IDispatcher Dispatcher { get; set; } = default!;

        [Inject]
        public IConfigRepository<TConfig> ConfigRepo { get; set; } = default!;

        [Inject]
        internal IState<ActiveConfig<TConfig>> ActiveConfig { get; set; } = default!;

        public TConfig? Selection
        {
            get => ActiveConfig.Value.Config;
            private set => Dispatcher.Dispatch(new ActiveConfig<TConfig>(value));
        }

        [Parameter]
        public string Label { get; set; } = "Select Server";

        [Parameter]
        public EventCallback<TConfig?> SelectionChanged { get; set; }

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            LocalStorage.Load<string>("selectedInstance", async savedSelection =>
            {
                var instanceToSelect = ConfigRepo.Configs.FirstOrDefault(c => c.BaseUrl == savedSelection);
                if (instanceToSelect == null)
                {
                    return;
                }

                await SetSelected(instanceToSelect, false);
            });
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await LocalStorage.Process();
            await base.OnAfterRenderAsync(firstRender);
        }

        private async Task OnSelectionChanged(TConfig selected)
        {
            await SetSelected(selected, true);
        }

        private async Task SetSelected(TConfig value, bool shouldSave)
        {
            Dispatcher.Dispatch(new ActiveConfig<TConfig>(value));
            if (shouldSave)
            {
                LocalStorage.Save("selectedInstance", Selection?.BaseUrl);
            }

            await SelectionChanged.InvokeAsync(Selection);
        }
    }
}
