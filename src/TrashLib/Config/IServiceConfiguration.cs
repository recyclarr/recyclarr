namespace TrashLib.Config
{
    public interface IServiceConfiguration
    {
        string ServiceId { get; }
        string BaseUrl { get; }
        string ApiKey { get; }
    }
}
