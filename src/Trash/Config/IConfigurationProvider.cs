namespace Trash.Config
{
    public interface IConfigurationProvider<T>
        where T : BaseConfiguration
    {
        T? ActiveConfiguration { get; set; }
    }
}
