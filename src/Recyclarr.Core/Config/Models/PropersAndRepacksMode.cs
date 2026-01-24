using System.Text.Json.Serialization;

namespace Recyclarr.Config.Models;

[JsonConverter(typeof(JsonStringEnumConverter<PropersAndRepacksMode>))]
public enum PropersAndRepacksMode
{
    PreferAndUpgrade,
    DoNotUpgrade,
    DoNotPrefer,
}
