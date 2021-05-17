using TrashLib.Radarr.Config;

namespace Recyclarr.Code.Radarr
{
    public class SelectableCustomFormat : SelectableItem<CustomFormatConfig>
    {
        public SelectableCustomFormat(CustomFormatConfig item, bool existsInGuide)
            : base(item)
        {
            ExistsInGuide = existsInGuide;
        }

        public bool ExistsInGuide { get; }
    }
}
