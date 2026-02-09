using System.IO.Abstractions;
using Recyclarr.Core.TestLibrary;
using Recyclarr.Json;
using Recyclarr.ResourceProviders.Domain;

namespace Recyclarr.Core.Tests.IntegrationTests;

[CoreDataSource]
internal sealed class CfGroupLoaderIntegrationTest(JsonResourceLoader sut, MockFileSystem fs)
{
    [Test]
    public void Load_cf_group_json_with_snake_case_works()
    {
        const string json = """
            {
                "name": "[Audio] Audio Formats",
                "trash_id": "9d5acd8f1da78dfbae788182f7605200",
                "trash_description": "Audio description",
                "default": "true",
                "custom_formats": [
                    {
                        "name": "TrueHD Atmos",
                        "trash_id": "496f355514737f7d83bf7aa4d24f8169",
                        "required": true,
                        "default": false
                    },
                    {
                        "name": "DTS X",
                        "trash_id": "2f22d89048b01681dde8afe203bf2e95",
                        "required": false,
                        "default": true
                    }
                ],
                "quality_profiles": {
                    "include": {
                        "HD Bluray + WEB": "d1d67249d3890e49bc12e275d989a7e9",
                        "SQP-1 (1080p)": "0896c29d74de619df168d23b98104b22"
                    }
                }
            }
            """;

        fs.AddFile("audio-formats.json", new MockFileData(json));

        IFileInfo[] files =
        [
            fs.FileInfo.New(fs.Path.Combine(fs.CurrentDirectory().FullName, "audio-formats.json")),
        ];

        var results = sut.Load<CfGroupResource>(files, GlobalJsonSerializerSettings.Metadata)
            .Select(t => t.Resource)
            .ToList();

        results.Should().ContainSingle();

        var group = results[0];
        group.Name.Should().Be("[Audio] Audio Formats");
        group.TrashId.Should().Be("9d5acd8f1da78dfbae788182f7605200");
        group.TrashDescription.Should().Be("Audio description");
        group.Default.Should().Be("true");

        group.CustomFormats.Should().HaveCount(2);
        group
            .CustomFormats.Should()
            .BeEquivalentTo([
                new CfGroupCustomFormat
                {
                    Name = "TrueHD Atmos",
                    TrashId = "496f355514737f7d83bf7aa4d24f8169",
                    Required = true,
                    Default = false,
                },
                new CfGroupCustomFormat
                {
                    Name = "DTS X",
                    TrashId = "2f22d89048b01681dde8afe203bf2e95",
                    Required = false,
                    Default = true,
                },
            ]);

        group.QualityProfiles.Include.Should().HaveCount(2);
        group
            .QualityProfiles.Include["HD Bluray + WEB"]
            .Should()
            .Be("d1d67249d3890e49bc12e275d989a7e9");
        group
            .QualityProfiles.Include["SQP-1 (1080p)"]
            .Should()
            .Be("0896c29d74de619df168d23b98104b22");
    }

    [Test]
    public void Load_multiple_cf_groups_deduplicates_by_trash_id()
    {
        const string json1 = """
            {
                "name": "First Group",
                "trash_id": "duplicate-id",
                "custom_formats": [],
                "quality_profiles": { "include": {} }
            }
            """;

        const string json2 = """
            {
                "name": "Second Group (should win)",
                "trash_id": "duplicate-id",
                "custom_formats": [],
                "quality_profiles": { "include": {} }
            }
            """;

        fs.AddFile("first.json", new MockFileData(json1));
        fs.AddFile("second.json", new MockFileData(json2));

        IFileInfo[] files =
        [
            fs.FileInfo.New(fs.Path.Combine(fs.CurrentDirectory().FullName, "first.json")),
            fs.FileInfo.New(fs.Path.Combine(fs.CurrentDirectory().FullName, "second.json")),
        ];

        var results = sut.Load<CfGroupResource>(files, GlobalJsonSerializerSettings.Metadata)
            .Select(t => t.Resource)
            .GroupBy(g => g.TrashId)
            .Select(g => g.Last())
            .ToList();

        results.Should().ContainSingle();
        results[0].Name.Should().Be("Second Group (should win)");
    }
}
