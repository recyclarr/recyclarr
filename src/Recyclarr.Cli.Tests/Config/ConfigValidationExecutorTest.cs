using FluentAssertions;
using NUnit.Framework;
using Recyclarr.Cli.Config;
using Recyclarr.Cli.TestLibrary;
using Recyclarr.TrashLib.TestLibrary;

namespace Recyclarr.Cli.Tests.Config;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class ConfigValidationExecutorTest : IntegrationFixture
{
    [Test]
    public void Invalid_returns_false()
    {
        var sut = Resolve<ConfigValidationExecutor>();
        var config = new TestConfig {ApiKey = ""}; // Use bad data

        var result = sut.Validate(config);

        result.Should().BeFalse();
    }

    [Test]
    public void Valid_returns_true()
    {
        var sut = Resolve<ConfigValidationExecutor>();
        var config = new TestConfig {ApiKey = "good", BaseUrl = "good"}; // Use good data

        var result = sut.Validate(config);

        result.Should().BeTrue();
    }
}
