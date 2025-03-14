using Recyclarr.Config.EnvironmentVariables;

namespace Recyclarr.Core.Tests.Config.EnvironmentVariables;

internal sealed class EnvironmentVariableNotDefinedExceptionTest
{
    [Test]
    public void Properties_get_initialized()
    {
        var sut = new EnvironmentVariableNotDefinedException(15, "key");
        sut.Line.Should().Be(15);
        sut.EnvironmentVariableName.Should().Be("key");
    }
}
