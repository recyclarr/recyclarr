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
        [Inject]
        public IDialogService DialogService { get; set; } = default!;

        [Inject]
        public IConfigPersister<RadarrConfiguration> ConfigPersister { get; set; } = default!;

        [Inject]
        public IConfigProvider<RadarrConfiguration> ConfigProvider { get; set; } = default!;

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
                ConfigProvider.Configs.Add(item);
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
            ConfigPersister.Save(ConfigProvider.Configs);
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
                ConfigProvider.Configs.Remove(item);
                SaveServers();
            }
        }
    }
}
