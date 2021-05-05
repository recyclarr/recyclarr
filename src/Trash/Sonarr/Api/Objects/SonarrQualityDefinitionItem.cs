using JetBrains.Annotations;

namespace Trash.Sonarr.Api.Objects
{
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public class SonarrQualityItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Source { get; set; } = "";
        public int Resolution { get; set; }
    }

    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public class SonarrQualityDefinitionItem
    {
        public int Id { get; set; }
        public SonarrQualityItem? Quality { get; set; }
        public string Title { get; set; } = "";
        public int Weight { get; set; }
        public decimal MinSize { get; set; }
        public decimal? MaxSize { get; set; }
    }
}
