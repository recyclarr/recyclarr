using Autofac;

namespace Recyclarr;

public interface ICompositionRoot
{
    IServiceLocatorProxy Setup(Action<ContainerBuilder>? extraRegistrations = null);
}
