namespace TrashLib.Config
{
    public abstract class ServiceConfiguration : IServiceConfiguration
    {
        public string BaseUrl { get; set; } = "";
        public string ApiKey { get; set; } = "";
    }
}
