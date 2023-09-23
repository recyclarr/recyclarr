using System.IO.Abstractions;
using Recyclarr.Json;
using Recyclarr.Repo;
using Recyclarr.TestLibrary;
using Recyclarr.TestLibrary.AutoFixture;
using Recyclarr.TrashGuide.ReleaseProfile;

namespace Recyclarr.TrashGuide.Tests.ReleaseProfile;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class ReleaseProfileGuideServiceTest
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

        var mockData1 = MakeMockObject("first");
        var mockData2 = MakeMockObject("second");
        var baseDir = fs.CurrentDirectory().SubDirectory("files");
        baseDir.Create();

        fs.AddFile(baseDir.File("first.json").FullName,
            MockData.FromJson(mockData1, GlobalJsonSerializerSettings.Services));

        fs.AddFile(baseDir.File("second.json").FullName,
            MockData.FromJson(mockData2, GlobalJsonSerializerSettings.Services));

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

        fs.AddFile(rootPath.File("0_bad_data.json").FullName,
            MockData.FromString(badData));

        fs.AddFile(rootPath.File("1_good_data.json").FullName,
            MockData.FromJson(goodData, GlobalJsonSerializerSettings.Services));

        metadataBuilder.ToDirectoryInfoList(default!).ReturnsForAnyArgs(new[] {rootPath});

        var results = sut.GetReleaseProfileData();

        results.Should().BeEquivalentTo(new[] {goodData});
    }
}
