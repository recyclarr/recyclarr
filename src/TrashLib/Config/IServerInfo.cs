namespace TrashLib.Config
{
    public interface IServerInfo
    {
        string ApiKey { get; }
        string BaseUrl { get; }
        string BuildUrl();
    }
}
