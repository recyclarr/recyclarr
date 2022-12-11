using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using NUnit.Framework;

namespace TrashLib.Tests;

[SetUpFixture]
[SuppressMessage("ReSharper", "CheckNamespace")]
public class GlobalTestSetup
{
    [OneTimeSetUp]
    public void Setup()
    {
        AssertionOptions.FormattingOptions.MaxDepth = 100;
    }
}
