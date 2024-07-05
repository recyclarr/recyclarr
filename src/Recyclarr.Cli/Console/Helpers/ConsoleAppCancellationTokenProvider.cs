namespace Recyclarr.Cli.Console.Helpers;

public interface IApplicationCancellationTokenProvider
{
    CancellationToken Token { get; }
}

public class ConsoleAppCancellationTokenProvider : IApplicationCancellationTokenProvider
{
    private CancellationToken? _token;

    public CancellationToken Token => _token ?? CancellationToken.None;

    public void SetToken(CancellationToken token)
    {
        _token = token;
    }

    public void ResetToken()
    {
        _token = null;
    }
}
