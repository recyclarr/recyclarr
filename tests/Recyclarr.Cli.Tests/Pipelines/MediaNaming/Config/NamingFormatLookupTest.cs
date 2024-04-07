using Recyclarr.Cli.Pipelines.MediaNaming.PipelinePhases.Config;

namespace Recyclarr.Cli.Tests.Pipelines.MediaNaming.Config;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class NamingFormatLookupTest
{
    [Test, AutoMockData]
    public void Assign_null_when_config_null(
        NamingFormatLookup sut)
    {
        var namingData = new Dictionary<string, string>
        {
            {"default", "folder_default"},
            {"plex", "folder_plex"},
            {"emby", "folder_emby"}
        };

        var result = sut.ObtainFormat(namingData, null, "");
        result.Should().BeNull();
    }
}
