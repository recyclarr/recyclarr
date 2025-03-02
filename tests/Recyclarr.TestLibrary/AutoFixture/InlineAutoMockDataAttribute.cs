using System.Diagnostics.CodeAnalysis;
using AutoFixture.NUnit4;

namespace Recyclarr.TestLibrary.AutoFixture;

[method: SuppressMessage(
    "Design",
    "CA1019",
    MessageId = "Define accessors for attribute arguments",
    Justification = "The parameter is forwarded to the base class and not used directly"
)]
public sealed class InlineAutoMockDataAttribute(params object?[] parameters)
    : InlineAutoDataAttribute(NSubstituteFixture.Create, parameters);
