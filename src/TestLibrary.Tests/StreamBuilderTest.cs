using FluentAssertions;
using NUnit.Framework;

namespace TestLibrary.Tests
{
    [TestFixture]
    [Parallelizable(ParallelScope.All)]
    public class StreamBuilderTest
    {
        [Test]
        public void FromString_UsingString_ShouldOutputSameString()
        {
            var stream = StreamBuilder.FromString("test");
            stream.ReadToEnd().Should().Be("test");
        }
    }
}
