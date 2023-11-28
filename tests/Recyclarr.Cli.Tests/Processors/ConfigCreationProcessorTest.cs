using System.Diagnostics.CodeAnalysis;
using Autofac.Extras.Ordering;
using AutoFixture;
using Recyclarr.Cli.Console.Commands;
using Recyclarr.Cli.Processors;
using Recyclarr.Cli.Processors.Config;

namespace Recyclarr.Cli.Tests.Processors;

[TestFixture]
public class ConfigCreationProcessorTest
{
    [SuppressMessage("Performance", "CA1812", Justification =
        "Used implicitly by test methods in this class")]
    private sealed class EmptyOrderedEnumerable : ICustomization
    {
        public void Customize(IFixture fixture)
        {
            fixture.Inject(Array.Empty<IConfigCreator>().AsOrdered());
        }
    }

    [Test, AutoMockData]
    public void Throw_when_no_config_creators_can_handle(
        [CustomizeWith(typeof(EmptyOrderedEnumerable))] ConfigCreationProcessor sut)
    {
        var settings = new ConfigCreateCommand.CliSettings();

        var act = () => sut.Process(settings);

        act.Should().Throw<FatalException>();
    }
}
