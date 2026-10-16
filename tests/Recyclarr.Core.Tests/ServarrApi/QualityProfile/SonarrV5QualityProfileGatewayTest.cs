using System.Text.Json;
using System.Text.Json.Nodes;
using Recyclarr.Json;
using Recyclarr.Servarr.QualityProfile;
using Recyclarr.ServarrApi.QualityProfile;
using Recyclarr.TestLibrary;
using SonarrApi = Recyclarr.Api.Sonarr.V5;

namespace Recyclarr.Core.Tests.ServarrApi.QualityProfile;

internal sealed class SonarrV5QualityProfileGatewayTest
{
    private const string ServerJson = """
        [{
          "id": 3,
          "name": "WEB-1080p",
          "cutoff": 1001,
          "formatItems": [{ "format": 10, "name": "CF", "score": 5 }],
          "items": [
            {
              "quality": { "id": 9, "name": "HDTV-1080p" },
              "items": [], "allowed": true,
              "minSize": 5, "maxSize": 50, "preferredSize": 30
            },
            {
              "id": 1001, "name": "WEB 1080p", "allowed": true,
              "items": [{
                "quality": { "id": 3, "name": "WEBDL-1080p" },
                "items": [], "allowed": true,
                "minSize": 4, "maxSize": 40, "preferredSize": 20
              }]
            }
          ]
        }]
        """;

    private readonly SonarrApi.IQualityProfileApi _api =
        Substitute.For<SonarrApi.IQualityProfileApi>();

    private SonarrV5QualityProfileGateway CreateSut()
    {
        _api.QualityprofileGet()
            .ReturnsForAnyArgs(
                JsonSerializer.Deserialize<List<SonarrApi.QualityProfileResource>>(
                    ServerJson,
                    GlobalJsonSerializerSettings.Services
                )!
            );

        return new SonarrV5QualityProfileGateway(
            new TestableLogger(),
            _api,
            Substitute.For<SonarrApi.IQualityProfileSchemaApi>()
        );
    }

    private JsonArray SentItems()
    {
        var sentDto = _api.ReceivedCalls()
            .Single(c =>
                c.GetMethodInfo().Name == nameof(SonarrApi.IQualityProfileApi.QualityprofilePut)
            )
            .GetArguments()[1];
        return JsonSerializer.SerializeToNode(sentDto, GlobalJsonSerializerSettings.Services)![
            "items"
        ]!.AsArray();
    }

    private static JsonNode QualityNode(JsonArray items, int qualityId) =>
        items
            .Select(x => x!)
            .SelectMany(x => x["quality"] is null ? x["items"]!.AsArray().Select(c => c!) : [x])
            .Single(x => x["quality"]!["id"]!.GetValue<int>() == qualityId);

    [Test]
    public async Task Fetch_maps_profile_and_item_sizes_to_domain()
    {
        var sut = CreateSut();

        var profile = (await sut.GetQualityProfiles(CancellationToken.None)).Single();

        profile.Name.Should().Be("WEB-1080p");
        profile.FormatItems.Should().ContainSingle().Which.FormatId.Should().Be(10);
        profile
            .Should()
            .BeOfType<SonarrV5QualityProfileData>()
            .Which.QualitySizes.Should()
            .BeEquivalentTo(
                new Dictionary<string, QualityItemSize>
                {
                    ["HDTV-1080p"] = new(5, 50, 30),
                    ["WEBDL-1080p"] = new(4, 40, 20),
                }
            );
    }

    [Test]
    public async Task Update_preserves_sizes_of_items_without_assigned_sizes()
    {
        var sut = CreateSut();
        var profile = (await sut.GetQualityProfiles(CancellationToken.None)).Single();

        await sut.UpdateQualityProfile(
            profile with
            {
                UpgradeAllowed = true,
            },
            CancellationToken.None
        );

        var items = SentItems();
        QualityNode(items, 9)["maxSize"]!.GetValue<double>().Should().Be(50);
        QualityNode(items, 3)["preferredSize"]!.GetValue<double>().Should().Be(20);
    }

    [Test]
    public async Task Update_applies_renamed_quality_group()
    {
        var sut = CreateSut();
        var profile = (await sut.GetQualityProfiles(CancellationToken.None)).Single();
        var items = profile.Items.ToList();
        var groupIndex = items.FindIndex(x => x.Id == 1001);
        items[groupIndex] = items[groupIndex] with { Name = "WEB 1080p Renamed" };

        await sut.UpdateQualityProfile(profile with { Items = items }, CancellationToken.None);

        SentItems()
            .Should()
            .ContainSingle(x => x!["id"] != null && x["id"]!.GetValue<int>() == 1001)
            .Which!["name"]!
            .GetValue<string>()
            .Should()
            .Be("WEB 1080p Renamed");
    }

    [Test]
    public async Task Regrouped_quality_keeps_its_server_sizes()
    {
        var sut = CreateSut();
        await sut.GetQualityProfiles(CancellationToken.None);

        // WEBDL-1080p leaves its group and becomes a top-level item; the domain carries no size.
        var regrouped = new SonarrV5QualityProfileData
        {
            Id = 3,
            Name = "WEB-1080p",
            Items =
            [
                new QualityProfileItem
                {
                    Quality = new QualityProfileItemQuality { Id = 9, Name = "HDTV-1080p" },
                    Allowed = true,
                },
                new QualityProfileItem
                {
                    Quality = new QualityProfileItemQuality { Id = 3, Name = "WEBDL-1080p" },
                    Allowed = true,
                },
            ],
        };

        await sut.UpdateQualityProfile(regrouped, CancellationToken.None);

        var webdl = QualityNode(SentItems(), 3);
        webdl["minSize"]!.GetValue<double>().Should().Be(4);
        webdl["maxSize"]!.GetValue<double>().Should().Be(40);
    }

    [Test]
    public async Task Update_writes_assigned_sizes_with_unlimited_as_absent()
    {
        var sut = CreateSut();
        var profile = (SonarrV5QualityProfileData)
            (await sut.GetQualityProfiles(CancellationToken.None)).Single();
        var sizes = new Dictionary<string, QualityItemSize>(profile.QualitySizes)
        {
            ["HDTV-1080p"] = new(6, null, 25),
        };

        await sut.UpdateQualityProfile(
            profile with
            {
                QualitySizes = sizes,
            },
            CancellationToken.None
        );

        var hdtv = QualityNode(SentItems(), 9);
        hdtv["minSize"]!.GetValue<double>().Should().Be(6);
        hdtv["maxSize"].Should().BeNull("unlimited is sent as an absent value");
        hdtv["preferredSize"]!.GetValue<double>().Should().Be(25);
    }
}
