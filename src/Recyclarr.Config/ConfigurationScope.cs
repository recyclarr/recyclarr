namespace Recyclarr.Config;

public abstract class ConfigurationScope : IDisposable
{
    private IDisposable? _scope;

    public void SetScope(IDisposable scope)
    {
        _scope = scope;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _scope?.Dispose();
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
