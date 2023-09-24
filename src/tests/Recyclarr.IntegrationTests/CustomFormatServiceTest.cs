using Flurl.Http.Testing;
using Recyclarr.Common;
using Recyclarr.Config.Models;
using Recyclarr.ServarrApi.Services;

namespace Recyclarr.IntegrationTests;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class CustomFormatServiceTest : IntegrationTestFixture
{
    [Test]
    public async Task Get_can_parse_json()
    {
        var resourceData = new ResourceDataReader(typeof(CustomFormatServiceTest), "Data");
        var jsonBody = resourceData.ReadData("issue_178.json");

        using var http = new HttpTest();
        http.RespondWith(jsonBody);

        var sut = Resolve<CustomFormatService>();
        var result = await sut.GetCustomFormats(Substitute.ForPartsOf<ServiceConfiguration>());

        result.Should().HaveCountGreaterThan(5);
    }
}
