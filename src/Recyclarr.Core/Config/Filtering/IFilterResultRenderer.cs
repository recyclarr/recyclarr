namespace Recyclarr.Config.Filtering;

public interface IFilterResultRenderer
{
    void RenderResults(IReadOnlyCollection<IFilterResult> results);
}
