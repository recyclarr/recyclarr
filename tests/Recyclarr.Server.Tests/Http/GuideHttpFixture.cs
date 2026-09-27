using System.IO.Abstractions;
using Recyclarr.Server.TestLibrary;

namespace Recyclarr.Server.Tests.Http;

// Serves guide data from a local trash-guides provider, so tests control the exact resources.
internal abstract class GuideHttpFixture : ServerHttpFixture
{
    private IDirectoryInfo GuideRoot => Fs.CurrentDirectory().SubDirectory("guide");

    [SetUp]
    public void AddLocalGuideProvider()
    {
        Fs.AddFile(
            GuideRoot.File("metadata.json"),
            new MockFileData(
                """
                {
                  "json_paths": {
                    "radarr": {
                      "custom_formats": ["radarr/cf"],
                      "qualities": ["radarr/quality-size"],
                      "naming": ["radarr/naming"],
                      "quality_profiles": ["radarr/quality-profiles"],
                      "custom_format_groups": ["radarr/cf-groups"]
                    },
                    "sonarr": {
                      "custom_formats": ["sonarr/cf"],
                      "qualities": ["sonarr/quality-size"],
                      "naming": ["sonarr/naming"],
                      "quality_profiles": ["sonarr/quality-profiles"],
                      "custom_format_groups": ["sonarr/cf-groups"]
                    }
                  }
                }
                """
            )
        );
        Fs.AddFile(
            Paths.ConfigDirectory.File("settings.yml"),
            new MockFileData(
                $"""
                resource_providers:
                  - name: local-guide
                    type: trash-guides
                    path: {GuideRoot.FullName}
                """
            )
        );
    }

    // Path is relative to the guide root, for example "radarr/cf/hdr.json".
    protected void AddGuideFile(string path, string json)
    {
        Fs.AddFile(GuideRoot.File(path), new MockFileData(json));
    }
}
