using System.Diagnostics.CodeAnalysis;
using Autofac.Extras.Ordering;
using AutoFixture;
using Recyclarr.Config;

namespace Recyclarr.Core.Tests.Config;

internal sealed class ConfigFileCreatorTest
{
    [SuppressMessage(
        "Performance",
        "CA1812",
        Justification = "Used implicitly by test methods in this class"
    )]
    private sealed class EmptyOrderedEnumerable : ICustomization
    {
        public void Customize(IFixture fixture)
        {
            fixture.Inject(Array.Empty<IConfigCreator>().AsOrdered());
        }
    }

    [Test, AutoMockData]
    public void Throw_when_no_config_creators_can_handle(
        [CustomizeWith(typeof(EmptyOrderedEnumerable))] ConfigFileCreator sut
    )
    {
        var settings = Substitute.For<ICreateConfigSettings>();

        var act = () => sut.Create(settings);

        act.Should().Throw<FatalException>();
    }
}
