using AutoMapper;
using Recyclarr.Cli.TestLibrary;

namespace Recyclarr.Cli.Tests;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class AutoMapperConfigurationTest : CliIntegrationFixture
{
    [Test]
    public void Automapper_config_is_valid()
    {
        var mapper = Resolve<MapperConfiguration>();
        mapper.AssertConfigurationIsValid();
    }
}
