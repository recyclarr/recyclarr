using System.IO.Abstractions;
using FluentValidation;
using FluentValidation.Results;
using Recyclarr.Common.FluentValidation;
using Recyclarr.Config.ExceptionTypes;
using Recyclarr.Config.Parsing.ErrorHandling;
using Recyclarr.ErrorHandling;
using Recyclarr.Migration;
using Recyclarr.Platform;
using Recyclarr.VersionControl;

namespace Recyclarr.Core.Tests.ErrorHandling;

internal sealed class ExceptionStrategyTest
{
    [Test]
    public async Task Config_strategy_retains_each_failure_variant()
    {
        var file = Substitute.For<IFileInfo>();
        file.Name.Returns("missing.yml");
        (Exception Exception, HandledInstanceFailure Expected)[] cases =
        [
            (new NoConfigurationFilesException(), new NoConfigurationFilesFailure()),
            (new InvalidInstancesException(["one"]), new InvalidInstancesFailure(["one"])),
            (new DuplicateInstancesException(["one"]), new DuplicateInstancesFailure(["one"])),
            (new SplitInstancesException(["one"]), new SplitInstancesFailure(["one"])),
            (
                new InvalidConfigurationFilesException([file]),
                new InvalidConfigurationFilesFailure(["missing.yml"])
            ),
            (new InvalidConfigurationException(), new InvalidConfigurationFailure()),
            (new PostProcessingException("post failed"), new PostProcessingFailure("post failed")),
        ];
        var sut = new ConfigExceptionStrategy();

        foreach (var (exception, expected) in cases)
        {
            var result = await sut.HandleAsync(exception);

            result.Should().BeEquivalentTo(expected);
        }
    }

    [Test]
    public async Task Environment_strategy_retains_message()
    {
        var sut = new EnvironmentExceptionStrategy();

        var result = await sut.HandleAsync(new EnvironmentException("missing home"));

        result.Should().Be(new EnvironmentFailure("missing home"));
    }

    [Test]
    public async Task Git_strategy_retains_exit_code()
    {
        var sut = new GitExceptionStrategy();

        var result = await sut.HandleAsync(new GitCmdException(exitCode: 2, "failed"));

        result.Should().Be(new GitFailure(ExitCode: 2));
    }

    [Test]
    public async Task Migration_strategy_retains_operation_reason_and_remediation()
    {
        var sut = new MigrationExceptionStrategy();
        var exception = new MigrationException(
            new IOException("disk full"),
            "move state",
            ["free space"]
        );

        var result = await sut.HandleAsync(exception);

        result
            .Should()
            .BeEquivalentTo(new MigrationFailure("move state", "disk full", ["free space"]));
    }

    [Test]
    public async Task Validation_strategy_retains_context_and_failure_details()
    {
        var failure = new ValidationFailure("Cutoff", "Invalid cutoff")
        {
            AttemptedValue = "bad",
            ErrorCode = "Rule",
        };
        var exception = new ContextualValidationException(
            new ValidationException([failure]),
            "WEB",
            "Profile validation"
        );
        var sut = new ValidationExceptionStrategy();

        var result = await sut.HandleAsync(exception);

        result
            .Should()
            .BeEquivalentTo(
                new ContextualValidationFailure(
                    "Profile validation",
                    "WEB",
                    [new ValidationFailureDetail("Cutoff", "Invalid cutoff", "bad", "Rule")]
                )
            );
    }

    [Test]
    public async Task Yaml_strategy_retains_file_line_and_detail()
    {
        var file = Substitute.For<IFileInfo>();
        file.Name.Returns("recyclarr.yml");
        var exception = new ConfigParsingException("invalid value", 5, new FormatException())
        {
            FilePath = file,
        };
        var sut = new YamlExceptionStrategy();

        var result = await sut.HandleAsync(exception);

        result.Should().Be(new ConfigParsingFailure("recyclarr.yml", 5, "invalid value"));
    }
}
