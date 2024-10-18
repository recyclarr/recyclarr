namespace Recyclarr.ServarrApi;

public static class DtoUtil
{
    public static void SetIfNotNull<T>(ref T propertyValue, T? newValue)
    {
        if (newValue is not null)
        {
            propertyValue = newValue;
        }
    }
}
