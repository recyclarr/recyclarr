namespace TrashLib.Config.Services;

public interface IConfigurationProvider
{
    IServiceConfiguration ActiveConfiguration { get; set; }
}
