namespace TrashLib.Config
{
    public interface IServiceConfiguration
    {
        int Id { get; set; }
        string BaseUrl { get; }
        string ApiKey { get; }

        string BuildUrl();
    }
}
