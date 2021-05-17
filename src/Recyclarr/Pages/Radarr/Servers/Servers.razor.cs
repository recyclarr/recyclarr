using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Recyclarr.Code.Settings.Persisters;
using TrashLib.Config;
using TrashLib.Radarr.Config;

namespace Recyclarr.Pages.Radarr.Servers
{
    [UsedImplicitly]
    public partial class Servers
    {
        private IList<RadarrConfiguration> _instances = default!;

        [Inject]
        public IDialogService DialogService { get; set; } = default!;

        [Inject]
        public IRadarrConfigPersister SettingsPersister { get; set; } = default!;

        protected override void OnInitialized()
        {
            _instances = SettingsPersister.Load();
        }

        private async Task<bool> ShowEditServerModal(string title, ServiceConfiguration instance)
        {
            var dlg = DialogService.Show<EditServerInstanceModal>(title,
                new DialogParameters
                {
                    ["BaseUrl"] = instance.BaseUrl,
                    ["ApiKey"] = instance.ApiKey
                },
                new DialogOptions
                {
                    MaxWidth = MaxWidth.Small
                });

            var result = await dlg.Result;
            if (result.Cancelled)
            {
                return false;
            }

            var (baseUrl, apiKey) = ((string BaseUrl, string ApiKey)) result.Data;
            instance.BaseUrl = baseUrl;
            instance.ApiKey = apiKey;
            return true;
        }

        private async Task OnAddServer()
        {
            var item = new RadarrConfiguration();
            if (await ShowEditServerModal("Add Server", item))
            {
                _instances.Add(item);
                SaveServers();
            }
        }

        private async Task OnEdit(RadarrConfiguration item)
        {
            await ShowEditServerModal("Edit Server", item);
            SaveServers();
        }

        private void SaveServers()
        {
            SettingsPersister.Save(_instances);
        }

        private async Task OnDelete(RadarrConfiguration item)
        {
            var shouldDelete = await DialogService.ShowMessageBox(
                "Warning",
                "Are you sure you want to delete the server? This cannot be undone!",
                "Delete",
                "Cancel");

            if (shouldDelete == true)
            {
                _instances.Remove(item);
                SaveServers();
            }
        }
    }
}
