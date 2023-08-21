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
        // Build an execution plan like:
        // var plan = mapper.BuildExecutionPlan(typeof(QualityProfileConfigYaml), typeof(QualityProfileConfig));
        // And do `plan.ToReadableString()` in the Debug Expressions/Watch
        mapper.AssertConfigurationIsValid();
    }
}
