using System.Reflection;
using AutoFixture;

namespace Recyclarr.TestLibrary.AutoFixture;

/// <summary>
/// Base class for attributes that customize AutoFixture specimens for test parameters.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = true)]
public abstract class CustomizeAttribute : Attribute
{
    /// <summary>
    /// Gets a customization for the specified parameter.
    /// </summary>
    public abstract ICustomization? GetCustomization(ParameterInfo parameter);
}
