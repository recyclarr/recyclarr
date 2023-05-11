using System.Reflection;
using Autofac;
using AutoFixture.Kernel;

namespace Recyclarr.TestLibrary.AutoFixture;

public class AutofacSpecimenBuilder : ISpecimenBuilder
{
    private readonly ILifetimeScope _container;

    public AutofacSpecimenBuilder(ILifetimeScope container)
    {
        _container = container;
    }

    public object Create(object request, ISpecimenContext context)
    {
        if (request is not ParameterInfo paramInfo)
        {
            return new NoSpecimen();
        }

        var instance = _container.ResolveOptional(paramInfo.ParameterType);
        return instance ?? new NoSpecimen();
    }
}
