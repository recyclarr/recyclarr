namespace Recyclarr.Common;

public class ConflictingYamlFilesException(IEnumerable<string> supportedFiles)
    : Exception(BuildMessage(supportedFiles))
{
    private static string BuildMessage(IEnumerable<string> supportedFiles)
    {
        return "Expected only 1 of the following files to exist, but found more than one: "
            + string.Join(", ", supportedFiles);
    }
}
