using Recyclarr.Core.TestLibrary;
using Recyclarr.ServarrApi.System;

namespace Recyclarr.Core.Tests.ServarrApi.System;

internal sealed class RadarrSystemGatewayTest : ServarrHttpTestFixture
{
    // Radarr 7 with native OIDC reports an authentication method older clients did not know
    [Test]
    public async Task Status_tolerates_values_unknown_to_recyclarr()
    {
        Respond(
            HttpMethod.Get,
            "/api/v3/system/status",
            """
            {
              "appName": "Radarr",
              "version": "7.0.0.10815",
              "authentication": "oidc",
              "databaseType": "futureDatabase"
            }
            """
        );
        var sut = ResolveGateway<RadarrSystemGateway>(NewConfig.Radarr());

        var result = await sut.GetStatus(CancellationToken.None);

        result.AppName.Should().Be("Radarr");
        result.Version.Should().Be(new Version("7.0.0.10815"));
    }
}
