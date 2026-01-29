using System.Reflection;
using AutoFixture;

namespace Recyclarr.TestLibrary.AutoFixture;

/// <summary>
/// Attribute that applies a custom ICustomization to a test parameter.
/// </summary>
/// <remarks>
/// Based on https://stackoverflow.com/a/16735551/157971
/// </remarks>
[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = true)]
public sealed class CustomizeWithAttribute : CustomizeAttribute
{
    public Type CustomizationType { get; }

    public CustomizeWithAttribute(Type customizationType)
    {
        ArgumentNullException.ThrowIfNull(customizationType);

        if (!typeof(ICustomization).IsAssignableFrom(customizationType))
        {
            throw new ArgumentException("Type needs to implement ICustomization");
        }

        CustomizationType = customizationType;
    }

    public override ICustomization? GetCustomization(ParameterInfo parameter)
    {
        return (ICustomization?)Activator.CreateInstance(CustomizationType);
    }
}
