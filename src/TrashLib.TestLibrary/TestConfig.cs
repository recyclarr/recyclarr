using JetBrains.Annotations;
using TrashLib.Config.Services;

namespace TrashLib.TestLibrary;

[UsedImplicitly]
public class TestConfig : ServiceConfiguration
{
    public TestConfig()
    {
        Name = "Test";
    }
}
