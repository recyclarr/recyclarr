using Serilog;
using TrashLib.Repo;
using TrashLib.Services.Common.QualityDefinition;

namespace TrashLib.Services.Sonarr.QualityDefinition;

internal class SonarrQualityGuideParser : ISonarrQualityGuideParser
{
    private readonly QualityGuideParser<SonarrQualityData> _parser;
    private readonly IRepoPathsFactory _pathFactory;

    public SonarrQualityGuideParser(ILogger log, IRepoPathsFactory pathFactory)
    {
        _parser = new QualityGuideParser<SonarrQualityData>(log);
        _pathFactory = pathFactory;
    }

    public ICollection<SonarrQualityData> GetQualities()
        => _parser.GetQualities(_pathFactory.Create().SonarrQualityPaths);
}
