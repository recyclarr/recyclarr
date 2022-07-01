using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using AutoFixture.NUnit3;
using FluentAssertions;
using MoreLinq.Extensions;
using NUnit.Framework;
using Recyclarr.Logging;
using TestLibrary.AutoFixture;
using TrashLib.TestLibrary;

namespace Recyclarr.Tests;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class LogJanitorTest
{
    [Test, AutoMockData]
    public void Keep_correct_number_of_newest_log_files(
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        [Frozen(Matching.ImplementedInterfaces)] TestAppPaths paths,
        LogJanitor janitor)
    {
        var testFiles = new[]
        {
            paths.LogDirectory.File("trash_2021-05-15_19-00-00.log"),
            paths.LogDirectory.File("trash_2021-05-15_20-00-00.log"),
            paths.LogDirectory.File("trash_2021-05-15_21-00-00.log"),
            paths.LogDirectory.File("trash_2021-05-15_22-00-00.log")
        };

        testFiles.ForEach(x => fs.AddFile(x.FullName, new MockFileData("")));

        janitor.DeleteOldestLogFiles(2);

        fs.AllFiles.Should().BeEquivalentTo(
            testFiles[2].FullName,
            testFiles[3].FullName);
    }
}
