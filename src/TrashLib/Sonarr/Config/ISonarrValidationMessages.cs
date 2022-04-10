namespace TrashLib.Sonarr.Config;

public interface ISonarrValidationMessages
{
    string BaseUrl { get; }
    string ApiKey { get; }
    string ReleaseProfileTrashIds { get; }
}
