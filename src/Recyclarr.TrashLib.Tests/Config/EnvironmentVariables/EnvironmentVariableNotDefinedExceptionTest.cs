using Recyclarr.TrashLib.Config.EnvironmentVariables;

namespace Recyclarr.TrashLib.Tests.Config.EnvironmentVariables;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class EnvironmentVariableNotDefinedExceptionTest
{
    [Test]
    public void Properties_get_initialized()
    {
        var sut = new EnvironmentVariableNotDefinedException(15, "key");
        sut.Line.Should().Be(15);
        sut.EnvironmentVariableName.Should().Be("key");
    }
}
