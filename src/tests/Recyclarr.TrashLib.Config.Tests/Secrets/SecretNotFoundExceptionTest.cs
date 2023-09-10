using Recyclarr.TrashLib.Config.Secrets;

namespace Recyclarr.TrashLib.Config.Tests.Secrets;

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
