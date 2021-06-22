namespace TrashLib.Config
{
    public interface IServiceConfiguration
    {
        string BaseUrl { get; }
        string ApiKey { get; }
        string BuildUrl();
    }
}
