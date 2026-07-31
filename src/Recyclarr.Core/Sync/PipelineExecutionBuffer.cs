using Recyclarr.Sync.Results;

namespace Recyclarr.Sync;

internal sealed class PipelineExecutionBuffer
{
    private readonly List<(PipelineType Type, PipelineResult Result)> _results = [];

    public IReadOnlyList<PipelineResult> Results =>
        _results.Select(x => x.Result).ToList().AsReadOnly();

    public void Capture(PipelineType type, PipelineResult result)
    {
        var index = _results.FindIndex(x => x.Type == type);
        if (index < 0)
        {
            _results.Add((type, result));
            return;
        }

        _results[index] = (type, result);
    }

    public PipelineResult? Get(PipelineType type)
    {
        var index = _results.FindIndex(x => x.Type == type);
        return index < 0 ? null : _results[index].Result;
    }
}
