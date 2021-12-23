using FluentAssertions;
using NUnit.Framework;
using TestLibrary.AutoFixture;
using TrashLib.Config.Settings;

namespace TrashLib.Tests.Config.Settings;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class SettingsProviderTest
{
    [Test, AutoMockData]
    public void Property_returns_same_value_from_set_method(SettingsProvider sut)
    {
        var settings = new SettingsValues();
        sut.UseSettings(settings);
        sut.Settings.Should().Be(settings);
    }
}
