using JetBrains.Annotations;

namespace Trash.Radarr.Api.Objects
{
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public class RadarrQualityItem
    {
        public int Id { get; set; }
        public string Modifier { get; set; } = "";
        public string Name { get; set; } = "";
        public string Source { get; set; } = "";
        public int Resolution { get; set; }
    }

    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public class RadarrQualityDefinitionItem
    {
        public int Id { get; set; }
        public RadarrQualityItem? Quality { get; set; }
        public string Title { get; set; } = "";
        public int Weight { get; set; }
        public decimal MinSize { get; set; }
        public decimal? MaxSize { get; set; }
        public decimal? PreferredSize { get; set; }
    }
}
