namespace Recyclarr.ServarrApi.QualityDefinition;

[UsedImplicitly(ImplicitUseKindFlags.Assign, ImplicitUseTargetFlags.WithMembers)]
public record ServiceQualityItem
{
    public int Id { get; init; }
    public string Modifier { get; init; } = "";
    public string Name { get; init; } = "";
    public string Source { get; init; } = "";
    public int Resolution { get; init; }
}

[UsedImplicitly(ImplicitUseKindFlags.Assign, ImplicitUseTargetFlags.WithMembers)]
public record ServiceQualityDefinitionItem
{
    public int Id { get; init; }
    public ServiceQualityItem? Quality { get; init; }
    public string Title { get; init; } = "";
    public int Weight { get; init; }
    public decimal MinSize { get; init; }
    public decimal? MaxSize { get; init; }
    public decimal? PreferredSize { get; init; }
}
