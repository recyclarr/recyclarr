using System.Diagnostics.CodeAnalysis;
using AutoFixture.NUnit3;

namespace TestLibrary.AutoFixture;

public sealed class InlineAutoMockDataAttribute : InlineAutoDataAttribute
{
    [SuppressMessage("Design", "CA1019", MessageId = "Define accessors for attribute arguments",
        Justification = "The parameter is forwarded to the base class and not used directly")]
    public InlineAutoMockDataAttribute(params object[] parameters)
        : base(NSubstituteFixture.Create, parameters)
    {
    }
}
