using Flurl.Http.Testing;
using Recyclarr.Cli.TestLibrary;
using Recyclarr.Common;
using Recyclarr.TrashLib.Config.Services;
using Recyclarr.TrashLib.Pipelines.CustomFormat.Api;

namespace Recyclarr.TrashLib.Tests.Pipelines.CustomFormat.Api;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class CustomFormatServiceTest : IntegrationFixture
{
    [Test, AutoMockData]
    public async Task Get_can_parse_json(IServiceConfiguration config)
    {
        var resourceData = new ResourceDataReader(typeof(CustomFormatServiceTest), "Data");
        var jsonBody = resourceData.ReadData("issue_178.json");

        using var http = new HttpTest();
        http.RespondWith(jsonBody);

        var sut = Resolve<CustomFormatService>();
        var result = await sut.GetCustomFormats(config);

        result.Should().HaveCountGreaterThan(5);
    }
}
