using System.IO.Abstractions;
using System.IO.Abstractions.Extensions;
using Newtonsoft.Json;
using Recyclarr.TestLibrary;
using Recyclarr.TrashLib.Pipelines.ReleaseProfile;
using Recyclarr.TrashLib.Pipelines.ReleaseProfile.Guide;
using Recyclarr.TrashLib.Repo;

namespace Recyclarr.TrashLib.Tests.Services;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class GuideServiceTest
{
    [Test, AutoMockData]
    public void Get_release_profile_json_works(
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        [Frozen] IRepoMetadataBuilder metadataBuilder,
        ReleaseProfileGuideService sut)
    {
        static ReleaseProfileData MakeMockObject(string term)
        {
            return new ReleaseProfileData
            {
                Name = "name",
                TrashId = "123",
                Required = new TermData[]
                {
                    new() {Term = term}
                }
            };
        }

        static MockFileData MockFileData(dynamic obj)
        {
            return new MockFileData(JsonConvert.SerializeObject(obj));
        }

        var mockData1 = MakeMockObject("first");
        var mockData2 = MakeMockObject("second");
        var baseDir = fs.CurrentDirectory().SubDirectory("files");
        baseDir.Create();

        fs.AddFile(baseDir.File("first.json").FullName, MockFileData(mockData1));
        fs.AddFile(baseDir.File("second.json").FullName, MockFileData(mockData2));

        metadataBuilder.ToDirectoryInfoList(default!).ReturnsForAnyArgs(new[] {baseDir});

        var results = sut.GetReleaseProfileData();

        results.Should().BeEquivalentTo(new[]
        {
            mockData1,
            mockData2
        });
    }

    [Test, AutoMockData]
    public void Json_exceptions_do_not_interrupt_parsing_other_files(
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        [Frozen] IRepoMetadataBuilder metadataBuilder,
        ReleaseProfileGuideService sut)
    {
        var rootPath = fs.CurrentDirectory().SubDirectory("files");
        rootPath.Create();

        var badData = "# comment";
        var goodData = new ReleaseProfileData
        {
            Name = "name",
            TrashId = "123",
            Required = new TermData[]
            {
                new() {Term = "abc"}
            }
        };

        fs.AddFile(rootPath.File("0_bad_data.json").FullName, MockData.FromString(badData));
        fs.AddFile(rootPath.File("1_good_data.json").FullName, MockData.FromJson(goodData));

        metadataBuilder.ToDirectoryInfoList(default!).ReturnsForAnyArgs(new[] {rootPath});

        var results = sut.GetReleaseProfileData();

        results.Should().BeEquivalentTo(new[] {goodData});
    }
}
