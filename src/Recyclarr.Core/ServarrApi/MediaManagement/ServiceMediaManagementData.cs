using System.Text.Json.Serialization;
using Recyclarr.Config.Models;

namespace Recyclarr.ServarrApi.MediaManagement;

public record ServiceMediaManagementData
{
    public int Id { get; init; } = 1;

    public PropersAndRepacksMode? DownloadPropersAndRepacks
    {
        get;
        init => DtoUtil.SetIfNotNull(ref field, value);
    }

    [UsedImplicitly]
    [JsonExtensionData]
    public Dictionary<string, object> ExtraJson { get; init; } = [];
}
