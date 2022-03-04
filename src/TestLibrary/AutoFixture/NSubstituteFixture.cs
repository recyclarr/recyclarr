using AutoFixture;
using AutoFixture.AutoNSubstitute;

namespace TestLibrary.AutoFixture;

public static class NSubstituteFixture
{
    public static Fixture Create()
    {
        var fixture = new Fixture
        {
            OmitAutoProperties = true
        };

        fixture.Customize(new AutoNSubstituteCustomization
        {
            ConfigureMembers = true
        });

        return fixture;
    }
}
