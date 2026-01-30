using System.Reflection;
using AutoFixture;
using AutoFixture.Kernel;

namespace Recyclarr.TestLibrary.AutoFixture;

/// <summary>
/// TUnit data source attribute that generates test method parameters using AutoFixture with NSubstitute.
/// Supports [Frozen] attribute for freezing specimens.
/// </summary>
public sealed class AutoMockDataAttribute : UntypedDataSourceGeneratorAttribute
{
    protected override IEnumerable<Func<object?[]?>> GenerateDataSources(
        DataGeneratorMetadata dataGeneratorMetadata
    )
    {
        yield return () => GenerateRow(dataGeneratorMetadata);
    }

    private static object?[] GenerateRow(DataGeneratorMetadata dataGeneratorMetadata)
    {
        var fixture = NSubstituteFixture.Create();

        var parameters = GetParameters(dataGeneratorMetadata);

        // Apply [CustomizeWith] and other CustomizeAttribute-derived customizations first
        ApplyCustomizeAttributes(fixture, parameters);

        // Then apply [Frozen] customizations
        ApplyFrozenCustomizations(fixture, parameters);

        var context = new SpecimenContext(fixture);
        return dataGeneratorMetadata
            .MembersToGenerate.Select(member => CreateSpecimen(member, context))
            .ToArray();
    }

    private static ParameterInfo[] GetParameters(DataGeneratorMetadata metadata)
    {
        return metadata
            .MembersToGenerate.OfType<ParameterMetadata>()
            .Select(p => p.ReflectionInfo)
            .ToArray();
    }

    private static void ApplyCustomizeAttributes(IFixture fixture, ParameterInfo[] parameters)
    {
        foreach (var param in parameters)
        {
            foreach (var attr in param.GetCustomAttributes<CustomizeAttribute>())
            {
                var customization = attr.GetCustomization(param);
                customization?.Customize(fixture);
            }
        }
    }

    private static void ApplyFrozenCustomizations(IFixture fixture, ParameterInfo[] parameters)
    {
        foreach (var param in parameters)
        {
            var frozenAttr = param.GetCustomAttribute<FrozenAttribute>();
            if (frozenAttr is null)
            {
                continue;
            }

            var matcher = BuildMatcher(param, frozenAttr.By);
            var customization = new FreezeOnMatchCustomization(param, matcher);
            customization.Customize(fixture);
        }
    }

    private static IRequestSpecification BuildMatcher(ParameterInfo param, Matching by)
    {
        var specs = new List<IRequestSpecification>
        {
            // Always match the exact parameter request
            new EqualRequestSpecification(param),
        };

        if (by.HasFlag(Matching.ExactType))
        {
            specs.Add(
                new OrRequestSpecification(
                    new ExactTypeSpecification(param.ParameterType),
                    new SeedRequestSpecification(param.ParameterType)
                )
            );
        }

        if (by.HasFlag(Matching.ImplementedInterfaces))
        {
            specs.Add(
                new AndRequestSpecification(
                    new InverseRequestSpecification(
                        new ExactTypeSpecification(param.ParameterType)
                    ),
                    new ImplementedInterfaceSpecification(param.ParameterType)
                )
            );
        }

        return specs.Count == 1 ? specs[0] : new OrRequestSpecification(specs);
    }

    private static object CreateSpecimen(IMemberMetadata member, SpecimenContext context)
    {
        // For test method parameters, resolve by ParameterInfo to support [Frozen] matching
        if (member is ParameterMetadata { ReflectionInfo: { } pi })
        {
            return context.Resolve(pi);
        }

        var type = member switch
        {
            PropertyMetadata prop => prop.Type,
            ParameterMetadata param => param.Type,
            ClassMetadata cls => cls.Type,
            MethodMetadata method => method.Type,
            _ => throw new InvalidOperationException($"Unknown member type: {member.GetType()}"),
        };

        return context.Resolve(type);
    }
}
