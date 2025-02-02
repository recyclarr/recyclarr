using FluentValidation;
using Recyclarr.Config.ExceptionTypes;
using Recyclarr.Config.Parsing;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Recyclarr.Config.Filtering;

public class InvalidInstancesFilter(IValidator<ServiceConfigYaml> validator) : IConfigFilter
{
    private class Result(IReadOnlyCollection<ConfigValidationErrorInfo> invalidInstances)
        : IFilterResult
    {
        public IRenderable Render()
        {
            return Text.Empty;
        }
    }

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
            context.AddResult(new Result(invalid));
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
