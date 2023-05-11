using Autofac;

namespace Recyclarr.TestLibrary.Autofac;

public static class AutofacTestExtensions
{
    public static void RegisterMockFor<T>(this ContainerBuilder builder) where T : class
    {
        builder.RegisterInstance(Substitute.For<T>()).As<T>();
    }

    public static void RegisterMockFor<T>(this ContainerBuilder builder, Action<T> mockSetup) where T : class
    {
        var mock = Substitute.For<T>();
        mockSetup(mock);
        builder.RegisterInstance(mock).As<T>();
    }
}
