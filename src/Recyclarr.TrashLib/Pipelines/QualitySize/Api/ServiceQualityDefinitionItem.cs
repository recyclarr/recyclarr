using JetBrains.Annotations;

namespace Recyclarr.TrashLib.Pipelines.QualitySize.Api;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class ServiceQualityItem
{
    public int Id { get; set; }
    public string Modifier { get; set; } = "";
    public string Name { get; set; } = "";
    public string Source { get; set; } = "";
    public int Resolution { get; set; }
}

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class ServiceQualityDefinitionItem
{
    public int Id { get; set; }
    public ServiceQualityItem? Quality { get; set; }
    public string Title { get; set; } = "";
    public int Weight { get; set; }
    public decimal MinSize { get; set; }
    public decimal? MaxSize { get; set; }
    public decimal? PreferredSize { get; set; }
}
