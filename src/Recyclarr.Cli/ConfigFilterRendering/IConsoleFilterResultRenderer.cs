using Recyclarr.Config.Filtering;
using Spectre.Console.Rendering;

namespace Recyclarr.Cli.ConfigFilterRendering;

internal interface IConsoleFilterResultRenderer
{
    Type CompatibleFilterResult { get; }
    IRenderable RenderResults(IFilterResult results);
}
