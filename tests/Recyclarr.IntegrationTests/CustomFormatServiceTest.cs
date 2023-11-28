using Flurl.Http.Testing;
using Recyclarr.Common;
using Recyclarr.Config.Models;
using Recyclarr.ServarrApi.CustomFormat;

namespace Recyclarr.IntegrationTests;

[TestFixture]
public class CustomFormatServiceTest : IntegrationTestFixture
{
    [Test]
    public async Task Get_can_parse_json()
    {
        var resourceData = new ResourceDataReader(typeof(CustomFormatServiceTest), "Data");
        var jsonBody = resourceData.ReadData("issue_178.json");
        var config = new RadarrConfiguration {InstanceName = "instance"};

        using var http = new HttpTest();
        http.RespondWith(jsonBody);

        var sut = Resolve<CustomFormatApiService>();
        var result = await sut.GetCustomFormats(config);

        result.Should().HaveCountGreaterThan(5);
    }
}
