using Recyclarr.Servarr.QualityProfile;
using Recyclarr.ServarrApi.QualityProfile;
using Recyclarr.TestLibrary;
using SonarrApi = Recyclarr.Api.Sonarr;

namespace Recyclarr.Core.Tests.ServarrApi.QualityProfile;

internal sealed class SonarrQualityProfileGatewayTest
{
    [Test]
    public async Task Update_preserves_renamed_quality_group()
    {
        // Simulates the TRaSH Guides anime profile bug: a quality group named "Bluray-1080p"
        // (same name as a child quality) is renamed to "Bluray 1080p" (space). The organizer
        // assigns the same ID to the new group (because the old group was filtered out, freeing
        // its ID). MergeItem must apply the new name, not the stashed old name.
        var originalDto = new SonarrApi.QualityProfileResource
        {
            Id = 8,
            Name = "Remux-1080p - Anime",
            UpgradeAllowed = true,
            Cutoff = 1004,
            FormatItems = [],
            Items =
            [
                new SonarrApi.QualityProfileQualityItemResource
                {
                    Quality = new SonarrApi.Quality { Id = 9, Name = "HDTV-1080p" },
                    Items = [],
                    Allowed = true,
                },
                new SonarrApi.QualityProfileQualityItemResource
                {
                    // Group named same as a child quality (the problematic pattern)
                    Id = 1004,
                    Name = "Bluray-1080p",
                    Allowed = true,
                    Items =
                    [
                        new SonarrApi.QualityProfileQualityItemResource
                        {
                            Quality = new SonarrApi.Quality { Id = 7, Name = "Bluray-1080p" },
                            Items = [],
                            Allowed = true,
                        },
                        new SonarrApi.QualityProfileQualityItemResource
                        {
                            Quality = new SonarrApi.Quality
                            {
                                Id = 20,
                                Name = "Bluray-1080p Remux",
                            },
                            Items = [],
                            Allowed = true,
                        },
                    ],
                },
            ],
        };

        var api = Substitute.For<SonarrApi.IQualityProfileApi>();
        var schemaApi = Substitute.For<SonarrApi.IQualityProfileSchemaApi>();
        var languageApi = Substitute.For<SonarrApi.ILanguageApi>();
        api.QualityprofileGet().ReturnsForAnyArgs([originalDto]);

        var sut = new SonarrQualityProfileGateway(
            new TestableLogger(),
            api,
            schemaApi,
            languageApi
        );

        // Fetch to populate stash
        await sut.GetQualityProfiles(CancellationToken.None);

        // Domain model after organizer processes the guide rename: group renamed from
        // "Bluray-1080p" to "Bluray 1080p", but still has the same ID (1004) because
        // AssignMissingGroupIds reused the filtered-out group's ID.
        var updatedDomain = new QualityProfileData
        {
            Id = 8,
            Name = "Remux-1080p - Anime",
            UpgradeAllowed = true,
            Cutoff = 1004,
            Items =
            [
                new QualityProfileItem
                {
                    Quality = new QualityProfileItemQuality { Id = 9, Name = "HDTV-1080p" },
                    Allowed = true,
                    Items = [],
                },
                new QualityProfileItem
                {
                    // Renamed group with same ID as the old group
                    Id = 1004,
                    Name = "Bluray 1080p",
                    Allowed = true,
                    Items =
                    [
                        new QualityProfileItem
                        {
                            Quality = new QualityProfileItemQuality
                            {
                                Id = 7,
                                Name = "Bluray-1080p",
                            },
                            Allowed = true,
                            Items = [],
                        },
                        new QualityProfileItem
                        {
                            Quality = new QualityProfileItemQuality
                            {
                                Id = 20,
                                Name = "Bluray-1080p Remux",
                            },
                            Allowed = true,
                            Items = [],
                        },
                    ],
                },
            ],
        };

        await sut.UpdateQualityProfile(updatedDomain, CancellationToken.None);

        // The PUT body should contain the NEW group name, not the stashed old name
        await api.ReceivedWithAnyArgs().QualityprofilePut(default!, default!);

        var putCall = api.ReceivedCalls()
            .First(c =>
                c.GetMethodInfo().Name == nameof(SonarrApi.IQualityProfileApi.QualityprofilePut)
            );
        var sentDto = (SonarrApi.QualityProfileResource)putCall.GetArguments()[1]!;

        var renamedGroup = sentDto.Items!.FirstOrDefault(i =>
            i.Quality is null && i.Name == "Bluray 1080p"
        );
        renamedGroup
            .Should()
            .NotBeNull(
                "the PUT body should contain the renamed group 'Bluray 1080p', "
                    + "not the stashed old name 'Bluray-1080p'"
            );
    }
}
