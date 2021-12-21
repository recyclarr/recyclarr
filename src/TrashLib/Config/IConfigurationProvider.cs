namespace TrashLib.Config;

public interface IConfigurationProvider
{
    IServiceConfiguration ActiveConfiguration { get; set; }
}
