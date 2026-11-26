using FluentValidation;
using FluentValidation.Results;
using Recyclarr.Common.FluentValidation;
using Recyclarr.Config.Parsing;
using Recyclarr.Core.TestLibrary;

namespace Recyclarr.Core.Tests.Config.Parsing;

internal sealed class ConfigValidationExecutorTest
{
    private readonly IRuntimeValidationService _validationService =
        Substitute.For<IRuntimeValidationService>();

    private ConfigValidationExecutor CreateSut() =>
        new(new ValidationLogger(Substitute.For<ILogger>()), _validationService);

    [Test]
    public void Return_false_on_validation_failure()
    {
        var sut = CreateSut();
        _validationService
            .Validate(default!)
            .ReturnsForAnyArgs(
                new ValidationResult([new ValidationFailure("property", "message")])
            );

        var result = sut.Validate(NewConfig.Radarr());

        result.Should().BeFalse();
    }

    [Test]
    public void Return_true_when_severity_is_warning()
    {
        var sut = CreateSut();
        _validationService
            .Validate(default!)
            .ReturnsForAnyArgs(
                new ValidationResult([
                    new ValidationFailure("property", "message") { Severity = Severity.Warning },
                ])
            );

        var result = sut.Validate(NewConfig.Radarr());

        result.Should().BeTrue();
    }

    [Test]
    public void Valid_returns_true()
    {
        var sut = CreateSut();
        _validationService.Validate(default!).ReturnsForAnyArgs(new ValidationResult());

        var result = sut.Validate(NewConfig.Radarr());

        result.Should().BeTrue();
    }
}
