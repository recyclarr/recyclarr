using System.IO.Abstractions.TestingHelpers;
using AutoFixture.NUnit3;
using FluentAssertions;
using MoreLinq.Extensions;
using NSubstitute;
using NUnit.Framework;
using Recyclarr.Logging;
using TestLibrary.AutoFixture;
using TrashLib;

namespace Recyclarr.Tests;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class LogJanitorTest
{
    [Test, AutoMockData]
    public void Keep_correct_number_of_newest_log_files(
        [Frozen] IAppPaths paths,
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        LogJanitor janitor)
    {
        const string logDir = "C:\\logs";
        paths.LogDirectory.Returns(logDir);

        var testFiles = new[]
        {
            $"{logDir}\\trash_2021-05-15_19-00-00",
            $"{logDir}\\trash_2021-05-15_20-00-00",
            $"{logDir}\\trash_2021-05-15_21-00-00",
            $"{logDir}\\trash_2021-05-15_22-00-00"
        };

        testFiles.ForEach(x => fs.AddFile(x, new MockFileData("")));

        janitor.DeleteOldestLogFiles(2);

        fs.FileExists(testFiles[2]).Should().BeTrue();
        fs.FileExists(testFiles[3]).Should().BeTrue();
    }
}
