namespace TrashLib.Config.Services;

public interface IServiceConfiguration
{
    string BaseUrl { get; }
    string ApiKey { get; }
    ICollection<CustomFormatConfig> CustomFormats { get; }
    bool DeleteOldCustomFormats { get; }
}
