using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Autofac;
using AutoFixture;
using AutoFixture.NUnit4;

namespace Recyclarr.TestLibrary.AutoFixture;

[SuppressMessage("Design", "CA1019", MessageId = "Define accessors for attribute arguments")]
public sealed class AutoMockDataAttribute : AutoDataAttribute
{
    public AutoMockDataAttribute()
        : base(NSubstituteFixture.Create) { }

    public AutoMockDataAttribute(Type testFixtureClass, string methodName)
        : base(() => CreateWithAutofac(testFixtureClass, methodName)) { }

    private static Fixture CreateWithAutofac(Type testFixtureClass, string methodName)
    {
        var method = testFixtureClass.GetMethod(
            methodName,
            BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static
        );
        var getContainer = method?.CreateDelegate<Func<ILifetimeScope>>();
        if (getContainer is null)
        {
            throw new ArgumentException(
                "Unable to find method on test fixture. Method must be non-public to be found.",
                nameof(methodName)
            );
        }

        var fixture = NSubstituteFixture.Create();
        fixture.Customizations.Add(new AutofacSpecimenBuilder(getContainer()));
        return fixture;
    }
}
