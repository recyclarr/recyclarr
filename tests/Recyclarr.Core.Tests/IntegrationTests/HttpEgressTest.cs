using Recyclarr.Api.Radarr;
using Recyclarr.Core.TestLibrary;
using Refit;

namespace Recyclarr.Core.Tests.IntegrationTests;

internal sealed class HttpEgressTest : IntegrationTestFixture
{
    [Test]
    public async Task Refit_clients_that_no_test_stubbed_cannot_reach_the_network()
    {
        using var scope = ResolveWithConfig<ISystemApi>(NewConfig.Radarr());

        var act = async () => await scope.Entry.Status();

        // Refit wraps handler exceptions, so assert on the message that reaches the test author.
        (await act.Should().ThrowAsync<ApiRequestException>()).WithMessage(
            "*attempted real HTTP egress*"
        );
    }
}
