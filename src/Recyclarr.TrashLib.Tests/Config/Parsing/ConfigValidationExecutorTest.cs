using System.Diagnostics.CodeAnalysis;
using Autofac;
using FluentValidation;
using Recyclarr.Cli.TestLibrary;
using Recyclarr.TrashLib.Config.Parsing;
using Recyclarr.TrashLib.Config.Services;
using Recyclarr.TrashLib.TestLibrary;

namespace Recyclarr.TrashLib.Tests.Config.Parsing;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class ConfigValidationExecutorTest : IntegrationFixture
{
    [SuppressMessage("Design", "CA1812", Justification = "Instantiated via reflection in unit test")]
    private sealed class TestValidator : AbstractValidator<ServiceConfiguration>
    {
        public bool ShouldSucceed { get; set; }

        public TestValidator()
        {
            RuleFor(x => x).Must(_ => ShouldSucceed);
        }
    }

    protected override void RegisterExtraTypes(ContainerBuilder builder)
    {
        builder.RegisterType<TestValidator>()
            .AsSelf()
            .As<IValidator<ServiceConfiguration>>()
            .SingleInstance();
    }

    [Test]
    public void Return_false_on_validation_failure()
    {
        var validator = Resolve<TestValidator>();
        validator.ShouldSucceed = false;

        var sut = Resolve<ConfigValidationExecutor>();

        var result = sut.Validate(new TestConfig());

        result.Should().BeFalse();
    }

    [Test]
    public void Valid_returns_true()
    {
        var validator = Resolve<TestValidator>();
        validator.ShouldSucceed = true;

        var sut = Resolve<ConfigValidationExecutor>();

        var result = sut.Validate(new TestConfig());

        result.Should().BeTrue();
    }
}
