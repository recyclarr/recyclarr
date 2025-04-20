using Recyclarr.Config.Filtering;
using Spectre.Console.Rendering;

namespace Recyclarr.Cli.ConfigFilterRendering;

internal abstract class TypedFilterResultRenderer<TFilter> : IConsoleFilterResultRenderer
    where TFilter : IFilterResult
{
    public Type CompatibleFilterResult => typeof(TFilter);

    public IRenderable RenderResults(IFilterResult results)
    {
        if (results is not TFilter filterResult)
        {
            throw new ArgumentException(
                $"Expected {typeof(TFilter).Name}, but got {results.GetType().Name}"
            );
        }

        return RenderResults(filterResult);
    }

    protected abstract IRenderable RenderResults(TFilter filterResult);
}
