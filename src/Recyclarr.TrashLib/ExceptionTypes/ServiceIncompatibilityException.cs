namespace Recyclarr.TrashLib.ExceptionTypes;

public class ServiceIncompatibilityException : Exception
{
    public ServiceIncompatibilityException(string msg)
        : base(msg)
    {
    }
}
