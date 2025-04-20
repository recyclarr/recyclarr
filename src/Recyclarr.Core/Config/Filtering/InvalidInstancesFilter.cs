using FluentValidation;
using FluentValidation.Results;
using Recyclarr.Config.Parsing;

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
