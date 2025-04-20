using FluentValidation;
using Recyclarr.Config.Filtering;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Recyclarr.Cli.ConfigFilterRendering;

internal sealed class InvalidInstancesFilterResultRenderer
    : TypedFilterResultRenderer<InvalidInstancesFilterResult>
{
    protected override IRenderable RenderResults(InvalidInstancesFilterResult filterResult)
    {
        var tree = new Tree("[orange1]Invalid Instances[/]");

        foreach (var (instanceName, failures) in filterResult.InvalidInstances)
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
