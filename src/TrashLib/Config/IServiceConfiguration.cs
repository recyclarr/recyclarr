namespace TrashLib.Config
{
    public interface IServiceConfiguration
    {
        string BaseUrl { get; init; }
        string ApiKey { get; init; }
        bool IsValid(out string msg);
    }
}
