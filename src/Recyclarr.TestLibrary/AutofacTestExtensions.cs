using Autofac;
using NSubstitute;

namespace Recyclarr.TestLibrary;

public static class AutofacTestExtensions
{
    public static void RegisterMockFor<T>(this ContainerBuilder builder) where T : class
    {
        builder.RegisterInstance(Substitute.For<T>()).As<T>();
    }
}
