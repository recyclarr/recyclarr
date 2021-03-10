namespace Trash.Config
{
    internal class ConfigurationProvider<T> : IConfigurationProvider<T>
        where T : BaseConfiguration
    {
        public T? ActiveConfiguration { get; set; }
    }
}
