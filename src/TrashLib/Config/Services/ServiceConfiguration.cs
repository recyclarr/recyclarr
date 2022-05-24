namespace TrashLib.Config.Services;

public abstract class ServiceConfiguration : IServiceConfiguration
{
    public string BaseUrl { get; init; } = "";
    public string ApiKey { get; init; } = "";
}
