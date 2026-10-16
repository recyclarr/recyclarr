using System.IO.Abstractions;
using Autofac;
using Recyclarr.Compatibility;
using Recyclarr.Compatibility.Sonarr;
using Recyclarr.Server.TestLibrary;
using SonarrV3Api = Recyclarr.Api.Sonarr.V3;
using SonarrV5Api = Recyclarr.Api.Sonarr.V5;

namespace Recyclarr.Server.Tests.Http;

// Instance calls detect the Sonarr version first, so each major reaches its own API generation.
[TestFixture("4.0.20.3012", false)]
[TestFixture("5.0.0.2569", true)]
internal sealed class InstanceCustomFormatsSonarrHttpTest(string version, bool expectV5)
    : ServerHttpFixture
{
    private readonly SonarrV3Api.ICustomFormatApi _v3 =
        Substitute.For<SonarrV3Api.ICustomFormatApi>();
    private readonly SonarrV5Api.ICustomFormatApi _v5 =
        Substitute.For<SonarrV5Api.ICustomFormatApi>();

    protected override void RegisterStubsAndMocks(ContainerBuilder builder)
    {
        var info = Substitute.For<IServiceInformation>();
        info.GetAppName(default).ReturnsForAnyArgs("Sonarr");
        info.GetVersion(default).ReturnsForAnyArgs(new Version(version));
        builder.RegisterInstance(info).As<IServiceInformation>();

        var capabilities = Substitute.For<ISonarrCapabilityFetcher>();
        capabilities
            .GetCapabilities(default)
            .ReturnsForAnyArgs(new SonarrCapabilities(new Version(version)));
        builder.RegisterInstance(capabilities).As<ISonarrCapabilityFetcher>();

        builder.RegisterInstance(_v3).As<SonarrV3Api.ICustomFormatApi>();
        builder.RegisterInstance(_v5).As<SonarrV5Api.ICustomFormatApi>();
    }

    private HttpClient StartWithInstance()
    {
        Fs.AddFile(
            Paths.ConfigDirectory.File("recyclarr.yml"),
            new MockFileData(
                """
                sonarr:
                  series:
                    base_url: http://localhost:8989
                    api_key: secret-api-key
                """
            )
        );
        return CreateClient();
    }

    [Test]
    public async Task List_uses_the_api_generation_of_the_detected_version()
    {
        using var client = StartWithInstance();

        using var response = await client.GetAsync(
            new Uri("/api/v1/instances/series/custom-formats", UriKind.Relative)
        );

        response.EnsureSuccessStatusCode();
        if (expectV5)
        {
            await _v5.ReceivedWithAnyArgs().CustomformatGet(default);
            await _v3.DidNotReceiveWithAnyArgs().CustomformatGet(default);
        }
        else
        {
            await _v3.ReceivedWithAnyArgs().CustomformatGet(default);
            await _v5.DidNotReceiveWithAnyArgs().CustomformatGet(default);
        }
    }

    [Test]
    public async Task Delete_uses_the_api_generation_of_the_detected_version()
    {
        using var client = StartWithInstance();

        using var response = await client.DeleteAsync(
            new Uri("/api/v1/instances/series/custom-formats/7", UriKind.Relative)
        );

        response.EnsureSuccessStatusCode();
        if (expectV5)
        {
            await _v5.ReceivedWithAnyArgs().CustomformatDelete(default, default);
            await _v3.DidNotReceiveWithAnyArgs().CustomformatDelete(default, default);
        }
        else
        {
            await _v3.ReceivedWithAnyArgs().CustomformatDelete(default, default);
            await _v5.DidNotReceiveWithAnyArgs().CustomformatDelete(default, default);
        }
    }
}
