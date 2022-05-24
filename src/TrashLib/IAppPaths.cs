namespace TrashLib;

public interface IAppPaths
{
    void SetAppDataPath(string path);
    string AppDataPath { get; }
    string ConfigPath { get; }
    string SettingsPath { get; }
    string LogDirectory { get; }
    string RepoDirectory { get; }
    string CacheDirectory { get; }
    string DefaultConfigFilename { get; }
}
