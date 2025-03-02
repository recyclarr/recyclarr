using System.Reflection;
using Autofac;
using AutoFixture.Kernel;

namespace Recyclarr.TestLibrary.AutoFixture;

internal class AutofacSpecimenBuilder(ILifetimeScope container) : ISpecimenBuilder
{
    public object Create(object request, ISpecimenContext context)
    {
        if (request is not ParameterInfo paramInfo)
        {
            return new NoSpecimen();
        }

        var instance = container.ResolveOptional(paramInfo.ParameterType);
        return instance ?? new NoSpecimen();
    }
}
