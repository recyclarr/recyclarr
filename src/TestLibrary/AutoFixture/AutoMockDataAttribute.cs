using AutoFixture.NUnit3;

namespace TestLibrary.AutoFixture;

public class AutoMockDataAttribute : AutoDataAttribute
{
    public AutoMockDataAttribute()
        : base(NSubstituteFixture.Create)
    {
    }
}
