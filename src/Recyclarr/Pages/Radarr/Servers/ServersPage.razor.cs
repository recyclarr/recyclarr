using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Recyclarr.Code.Radarr;
using TrashLib.Config;
using TrashLib.Radarr.Config;

namespace Recyclarr.Pages.Radarr.Servers
{
    [UsedImplicitly]
    public partial class ServersPage
    {
        [Inject]
        public IDialogService DialogService { get; set; } = default!;

        [Inject]
        public IConfigRepository<RadarrConfig> ConfigRepo { get; set; } = default!;

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
            var item = new RadarrConfig();
            if (await ShowEditServerModal("Add Server", item))
            {
                ConfigRepo.Add(item);
                SaveServers();
            }
        }

        private async Task OnEdit(RadarrConfig item)
        {
            await ShowEditServerModal("Edit Server", item);
            SaveServers();
        }

        private void SaveServers()
        {
            ConfigRepo.Save();
        }

        private async Task OnDelete(RadarrConfig item)
        {
            var shouldDelete = await DialogService.ShowMessageBox(
                "Warning",
                "Are you sure you want to delete the server? This cannot be undone!",
                "Delete",
                "Cancel");

            if (shouldDelete == true)
            {
                ConfigRepo.Remove(item);
                SaveServers();
            }
        }
    }
}
