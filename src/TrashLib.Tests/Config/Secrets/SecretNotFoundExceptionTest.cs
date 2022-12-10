using FluentAssertions;
using NUnit.Framework;
using TrashLib.Config.Secrets;

namespace TrashLib.Tests.Config.Secrets;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class SecretNotFoundExceptionTest
{
    [Test]
    public void Properties_get_initialized()
    {
        var sut = new SecretNotFoundException(15, "key");
        sut.Line.Should().Be(15);
        sut.SecretKey.Should().Be("key");
    }
}
