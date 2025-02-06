using FluentValidation;
using FluentValidation.Results;
using Recyclarr.Config.Parsing;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Recyclarr.Config.Filtering;

public record ConfigValidationErrorInfo(
    string InstanceName,
    IReadOnlyCollection<ValidationFailure> Failures
);

public class InvalidInstancesFilter(ILogger log, IValidator<ServiceConfigYaml> validator)
    : IConfigFilter
{
    public IReadOnlyCollection<LoadedConfigYaml> Filter(
        ConfigFilterCriteria criteria,
        IReadOnlyCollection<LoadedConfigYaml> configs,
        FilterContext context
    )
    {
        var invalid = configs
            .Select(config =>
                (
                    config.InstanceName,
                    Result: validator.Validate(
                        config.Yaml,
                        options =>
                            options
                                .IncludeRulesNotInRuleSet()
                                .IncludeRuleSets(YamlValidatorRuleSets.RootConfig)
                    )
                )
            )
            .Where(x => !x.Result.IsValid)
            .Select(r => new ConfigValidationErrorInfo(r.InstanceName, r.Result.Errors))
            .ToList();

        if (invalid.Count != 0)
        {
            context.AddResult(new InvalidInstancesFilterResult(invalid));
            log.Debug(
                "Invalid instances: {@Instances}",
                invalid.Select(x => new
                {
                    x.InstanceName,
                    Errors = x.Failures.Select(f => f.ErrorMessage),
                })
            );
        }

        return configs
            .ExceptBy(
                invalid.Select(x => x.InstanceName),
                x => x.InstanceName,
                StringComparer.InvariantCultureIgnoreCase
            )
            .ToList();
    }
}

public class InvalidInstancesFilterResult(
    IReadOnlyCollection<ConfigValidationErrorInfo> invalidInstances
) : IFilterResult
{
    public IReadOnlyCollection<ConfigValidationErrorInfo> InvalidInstances => invalidInstances;

    public IRenderable Render()
    {
        var tree = new Tree("[orange1]Invalid Instances[/]");

        foreach (var (instanceName, failures) in invalidInstances)
        {
            var instanceNode = tree.AddNode($"[cornflowerblue]{instanceName}[/]");

            foreach (var f in failures)
            {
                var prefix = GetSeverityPrefix(f.Severity);
                instanceNode.AddNode($"{prefix} {f.ErrorMessage}");
            }
        }

        return tree;
    }

    private static string GetSeverityPrefix(Severity severity)
    {
        return severity switch
        {
            Severity.Error => "[red]X[/]",
            Severity.Warning => "[yellow]![/]",
            Severity.Info => "[blue]i[/]",
            _ => "[grey]?[/]",
        };
    }
}
