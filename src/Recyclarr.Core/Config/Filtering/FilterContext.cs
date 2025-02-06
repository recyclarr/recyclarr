namespace Recyclarr.Config.Filtering;

public class FilterContext
{
    private readonly List<IFilterResult> _results = [];

    public IReadOnlyCollection<IFilterResult> Results => _results;

    public void AddResult(IFilterResult result) => _results.Add(result);
}
