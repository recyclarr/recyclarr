namespace Recyclarr.Cli.Pipelines.CustomFormat.Cache;

public interface ICustomFormatCachePersister
{
    CustomFormatCache Load();
    void Save(CustomFormatCache cache);
}
