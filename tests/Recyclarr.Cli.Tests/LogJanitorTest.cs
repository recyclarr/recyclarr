using System.IO.Abstractions;
using Recyclarr.Cli.Logging;
using Recyclarr.Platform;

namespace Recyclarr.Cli.Tests;

[TestFixture]
public class LogJanitorTest
{
    [Test, AutoMockData]
    public void Keep_correct_number_of_newest_log_files(
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        [Frozen(Matching.ImplementedInterfaces)] AppPaths paths,
        LogJanitor janitor)
    {
        var testFiles = new[]
            {
                "trash_2021-05-15_19-00-00.log",
                "trash_2021-05-15_20-00-00.log",
                "trash_2021-05-15_21-00-00.log",
                "trash_2021-05-15_22-00-00.log"
            }
            .Select(x => paths.LogDirectory.File(x))
            .ToList();

        foreach (var file in testFiles)
        {
            fs.AddEmptyFile(file);
        }

        janitor.DeleteOldestLogFiles(2);

        fs.AllFiles.Should().BeEquivalentTo(
            testFiles[2].FullName,
            testFiles[3].FullName);
    }
}
