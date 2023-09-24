namespace Recyclarr.Compatibility;

public class ServiceIncompatibilityException : Exception
{
    public ServiceIncompatibilityException(string msg)
        : base(msg)
    {
    }
}
