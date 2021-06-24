using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Recyclarr.Code.Radarr;

namespace Recyclarr.Pages.Radarr.CustomFormats
{
    public partial class CustomFormatAccessLayout
    {
        private string _exceptionMsg = "";
        private bool _exceptionOccurred;
        public Action? OnReload { get; set; }

        [Inject]
        public IGuideProcessor GuideProcessor { get; set; } = default!;

        public bool IsLoaded => GuideProcessor.IsLoaded;

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            await Reload();
        }

        private async Task RequestCustomFormats(bool force)
        {
            try
            {
                StateHasChanged();
                _exceptionOccurred = false;
                var wasLoaded = true;

                if (force)
                {
                    await GuideProcessor.ForceBuildGuideData();
                }
                else
                {
                    wasLoaded = await GuideProcessor.BuildRepository();
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
