using Serilog;
using TrashLib.Repo;
using TrashLib.Services.Common.QualityDefinition;
using TrashLib.Services.CustomFormat.Guide;
using TrashLib.Services.CustomFormat.Models;
using TrashLib.Services.Radarr.QualityDefinition;

namespace TrashLib.Services.Radarr;

public class LocalRepoRadarrGuideService : IRadarrGuideService
{
    private readonly IRepoPathsFactory _pathsFactory;
    private readonly ICustomFormatLoader _cfLoader;
    private readonly QualityGuideParser<RadarrQualityData> _parser;

    public LocalRepoRadarrGuideService(IRepoPathsFactory pathsFactory, ILogger log, ICustomFormatLoader cfLoader)
    {
        _pathsFactory = pathsFactory;
        _cfLoader = cfLoader;
        _parser = new QualityGuideParser<RadarrQualityData>(log);
    }

    public ICollection<RadarrQualityData> GetQualities()
        => _parser.GetQualities(_pathsFactory.Create().RadarrQualityPaths);

    public ICollection<CustomFormatData> GetCustomFormatData()
    {
        var paths = _pathsFactory.Create();
        return _cfLoader.LoadAllCustomFormatsAtPaths(paths.RadarrCustomFormatPaths);
    }
}
