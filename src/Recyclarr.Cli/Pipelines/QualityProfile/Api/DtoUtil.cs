namespace Recyclarr.Cli.Pipelines.QualityProfile.Api;

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
