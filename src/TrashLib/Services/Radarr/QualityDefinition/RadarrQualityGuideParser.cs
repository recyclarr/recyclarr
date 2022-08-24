using Serilog;
using TrashLib.Repo;
using TrashLib.Services.Common.QualityDefinition;

namespace TrashLib.Services.Radarr.QualityDefinition;

internal class RadarrQualityGuideParser : IRadarrQualityGuideParser
{
    private readonly QualityGuideParser<RadarrQualityData> _parser;
    private readonly IRepoPathsFactory _pathFactory;

    public RadarrQualityGuideParser(ILogger log, IRepoPathsFactory pathFactory)
    {
        _parser = new QualityGuideParser<RadarrQualityData>(log);
        _pathFactory = pathFactory;
    }

    public ICollection<RadarrQualityData> GetQualities()
        => _parser.GetQualities(_pathFactory.Create().RadarrQualityPaths);
}
