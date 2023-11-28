using AutoMapper;

namespace Recyclarr.Cli.IntegrationTests;

[TestFixture]
internal class AutoMapperConfigurationTest : CliIntegrationFixture
{
    [Test]
    public void Automapper_config_is_valid()
    {
        var mapper = Resolve<MapperConfiguration>();
        // Build an execution plan like:
        // var plan = mapper.BuildExecutionPlan(typeof(QualityProfileConfigYaml), typeof(QualityProfileConfig));
        // And do `plan.ToReadableString()` in the Debug Expressions/Watch
        mapper.AssertConfigurationIsValid();
    }
}
