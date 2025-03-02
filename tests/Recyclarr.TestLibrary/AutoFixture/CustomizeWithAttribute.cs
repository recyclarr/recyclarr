using System.Reflection;
using AutoFixture;
using AutoFixture.NUnit4;

namespace Recyclarr.TestLibrary.AutoFixture;

// Based on the answer here: https://stackoverflow.com/a/16735551/157971
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
