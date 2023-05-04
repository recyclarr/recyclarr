namespace Recyclarr.TrashLib.Settings;

public interface IRepositorySettings
{
    Uri CloneUrl { get; }
    string Branch { get; }
    string? Sha1 { get; }
}
