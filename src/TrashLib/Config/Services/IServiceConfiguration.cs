namespace TrashLib.Config.Services;

public interface IServiceConfiguration
{
    string? Name { get; }
    string BaseUrl { get; }
    string ApiKey { get; }
    ICollection<CustomFormatConfig> CustomFormats { get; }
    bool DeleteOldCustomFormats { get; }
}
