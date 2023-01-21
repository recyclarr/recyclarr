using Recyclarr.TrashLib.Repo;
using Recyclarr.TrashLib.Services.CustomFormat.Guide;
using Recyclarr.TrashLib.Services.CustomFormat.Models;
using Recyclarr.TrashLib.Services.QualitySize;
using Recyclarr.TrashLib.Services.QualitySize.Guide;
using Serilog;

namespace Recyclarr.TrashLib.Services.Radarr;

public class LocalRepoRadarrGuideService : RadarrGuideService
{
    private readonly IRepoPathsFactory _pathsFactory;
    private readonly ICustomFormatLoader _cfLoader;
    private readonly QualitySizeGuideParser<QualitySizeData> _parser;

    public LocalRepoRadarrGuideService(IRepoPathsFactory pathsFactory, ILogger log, ICustomFormatLoader cfLoader)
    {
        _pathsFactory = pathsFactory;
        _cfLoader = cfLoader;
        _parser = new QualitySizeGuideParser<QualitySizeData>(log);
    }

    public override ICollection<QualitySizeData> GetQualities()
    {
        return _parser.GetQualities(_pathsFactory.Create().RadarrQualityPaths);
    }

    public override ICollection<CustomFormatData> GetCustomFormatData()
    {
        var paths = _pathsFactory.Create();
        return _cfLoader.LoadAllCustomFormatsAtPaths(
            paths.RadarrCustomFormatPaths,
            paths.RadarrCollectionOfCustomFormats);
    }
}
