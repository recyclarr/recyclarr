using Recyclarr.Config.Secrets;

namespace Recyclarr.Tests.Config.Secrets;

[TestFixture]
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
