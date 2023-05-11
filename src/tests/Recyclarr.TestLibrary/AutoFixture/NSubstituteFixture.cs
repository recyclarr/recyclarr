using AutoFixture;
using AutoFixture.AutoNSubstitute;

namespace Recyclarr.TestLibrary.AutoFixture;

public static class NSubstituteFixture
{
    public static Fixture Create()
    {
        var fixture = new Fixture
        {
            OmitAutoProperties = true
        };

        fixture
            .Customize(new AutoNSubstituteCustomization {ConfigureMembers = true})
            .Customize(new MockFileSystemSpecimenBuilder());

        return fixture;
    }
}
