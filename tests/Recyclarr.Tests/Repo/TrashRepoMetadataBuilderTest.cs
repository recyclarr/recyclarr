using System.IO.Abstractions;
using Recyclarr.Repo;

namespace Recyclarr.Tests.Repo;

[TestFixture]
public class TrashRepoMetadataBuilderTest
{
    private const string MetadataJson =
        """
        {
          "$schema": "metadata.schema.json",
          "json_paths": {
            "radarr": {
              "custom_formats": ["docs/json/radarr/cf"],
              "qualities": ["docs/json/radarr/quality-size"],
              "naming": ["docs/json/radarr/naming"]
            },
            "sonarr": {
              "release_profiles": ["docs/json/sonarr/rp"],
              "custom_formats": ["docs/json/sonarr/cf"],
              "qualities": ["docs/json/sonarr/quality-size"],
              "naming": ["docs/json/sonarr/naming"]
            }
          },
          "recyclarr": {
            "templates": "docs/recyclarr-configs"
          }
        }
        """;

    [Test, AutoMockData]
    public void Naming_is_parsed(
        [Frozen] ITrashGuidesRepo repo,
        MockFileSystem fs,
        TrashRepoMetadataBuilder sut)
    {
        fs.AddFile(repo.Path.File("metadata.json"), new MockFileData(MetadataJson));

        var result = sut.GetMetadata();

        result.JsonPaths.Radarr.Naming.Should().BeEquivalentTo("docs/json/radarr/naming");
        result.JsonPaths.Sonarr.Naming.Should().BeEquivalentTo("docs/json/sonarr/naming");
    }
}
