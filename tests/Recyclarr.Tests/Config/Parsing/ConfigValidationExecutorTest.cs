using FluentValidation;
using FluentValidation.Results;
using Recyclarr.Common.FluentValidation;
using Recyclarr.Config.Parsing;
using Recyclarr.Tests.TestLibrary;

namespace Recyclarr.Tests.Config.Parsing;

[TestFixture]
public class ConfigValidationExecutorTest
{
    [Test, AutoMockData]
    public void Return_false_on_validation_failure(
        [Frozen] IRuntimeValidationService validationService,
        ConfigValidationExecutor sut)
    {
        validationService.Validate(default!).ReturnsForAnyArgs(new ValidationResult(new[]
        {
            new ValidationFailure("property", "message")
        }));

        var result = sut.Validate(NewConfig.Radarr());

        result.Should().BeFalse();
    }

    [Test, AutoMockData]
    public void Return_true_when_severity_is_warning(
        [Frozen] IRuntimeValidationService validationService,
        ConfigValidationExecutor sut)
    {
        validationService.Validate(default!).ReturnsForAnyArgs(new ValidationResult(new[]
        {
            new ValidationFailure("property", "message") {Severity = Severity.Warning}
        }));

        var result = sut.Validate(NewConfig.Radarr());

        result.Should().BeTrue();
    }

    [Test, AutoMockData]
    public void Valid_returns_true(
        [Frozen] IRuntimeValidationService validationService,
        ConfigValidationExecutor sut)
    {
        validationService.Validate(default!).ReturnsForAnyArgs(new ValidationResult());

        var result = sut.Validate(NewConfig.Radarr());

        result.Should().BeTrue();
    }
}
