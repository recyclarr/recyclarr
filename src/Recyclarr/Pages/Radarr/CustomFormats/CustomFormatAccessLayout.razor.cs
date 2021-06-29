using System;
using System.Threading.Tasks;
using Fluxor;
using Microsoft.AspNetCore.Components;
using Recyclarr.Code.Radarr;
using Recyclarr.Code.Radarr.Fluxor;
using TrashLib.Radarr.Config;

namespace Recyclarr.Pages.Radarr.CustomFormats
{
    public partial class CustomFormatAccessLayout
    {
        private string _exceptionMsg = "";
        private bool _exceptionOccurred;
        public Action? OnReload { get; set; }

        [Inject]
        public IGuideProcessor GuideProcessor { get; set; } = default!;

        [Inject]
        private IState<ActiveConfig<RadarrConfig>> ActiveConfig { get; set; } = default!;

        public bool IsLoaded => GuideProcessor.IsLoaded;

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
        }

        private async Task RequestCustomFormats(bool force)
        {
            try
            {
                var config = ActiveConfig.Value.Config;
                if (config == null)
                {
                    return;
                }

                StateHasChanged();
                _exceptionOccurred = false;
                var wasLoaded = true;

                if (force)
                {
                    await GuideProcessor.ForceBuildGuideData(config);
                }
                else
                {
                    wasLoaded = await GuideProcessor.BuildGuideData(config);
                }

                if (wasLoaded)
                {
                    OnReload?.Invoke();
                }
            }
            catch (Exception e)
            {
                _exceptionMsg = e.Message;
                _exceptionOccurred = true;
            }

            StateHasChanged();
        }

        public async Task Reload()
        {
            await RequestCustomFormats(false);
        }

        public async Task ForceReload()
        {
            await RequestCustomFormats(true);
        }
    }
}
