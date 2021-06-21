namespace TrashLib.Config
{
    public abstract class ServiceConfiguration : IServiceConfiguration
    {
        public abstract string ServiceId { get; }
        public string BaseUrl { get; set; } = "";
        public string ApiKey { get; set; } = "";
    }
}
