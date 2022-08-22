namespace TrashLib.Services.Sonarr.Config;

public interface ISonarrValidationMessages
{
    string BaseUrl { get; }
    string ApiKey { get; }
    string ReleaseProfileTrashIds { get; }
}
