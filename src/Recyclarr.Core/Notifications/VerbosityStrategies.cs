namespace Recyclarr.Notifications;

public interface IVerbosityStrategy
{
    bool ShouldSendInformation();
    bool ShouldSendError();
    bool ShouldSendWarning();
    bool ShouldSendEmpty();
}

public class MinimalVerbosityStrategy : IVerbosityStrategy
{
    public bool ShouldSendInformation() => false;

    public bool ShouldSendError() => true;

    public bool ShouldSendWarning() => true;

    public bool ShouldSendEmpty() => false;
}

public class NormalVerbosityStrategy : IVerbosityStrategy
{
    public bool ShouldSendInformation() => true;

    public bool ShouldSendError() => true;

    public bool ShouldSendWarning() => true;

    public bool ShouldSendEmpty() => false;
}

public class DetailedVerbosityStrategy : IVerbosityStrategy
{
    public bool ShouldSendInformation() => true;

    public bool ShouldSendError() => true;

    public bool ShouldSendWarning() => true;

    public bool ShouldSendEmpty() => true;
}
