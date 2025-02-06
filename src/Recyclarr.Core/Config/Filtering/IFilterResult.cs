using Spectre.Console.Rendering;

namespace Recyclarr.Config.Filtering;

public interface IFilterResult
{
    IRenderable Render();
}
