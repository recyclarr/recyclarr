namespace TrashLib.Config.Services;

public interface IServiceValidationMessages
{
    string BaseUrl { get; }
    string ApiKey { get; }
    string CustomFormatTrashIds { get; }
}
