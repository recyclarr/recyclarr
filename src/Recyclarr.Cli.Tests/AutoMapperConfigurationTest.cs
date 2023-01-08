using AutoMapper;
using NUnit.Framework;
using Recyclarr.Cli.TestLibrary;

namespace Recyclarr.Cli.Tests;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class AutoMapperConfigurationTest : IntegrationFixture
{
    [Test]
    public void Automapper_config_is_valid()
    {
        var mapper = Resolve<MapperConfiguration>();
        mapper.AssertConfigurationIsValid();
    }
}
