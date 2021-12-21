using System.IO.Abstractions;
using NSubstitute;
using NUnit.Framework;

namespace Trash.Tests;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class LogJanitorTest
{
    [Test]
    public void Keep_correct_number_of_newest_log_files()
    {
        var fs = Substitute.For<IFileSystem>();
        var janitor = new LogJanitor(fs);

        var testFileInfoList = new[]
        {
            Substitute.For<IFileInfo>(),
            Substitute.For<IFileInfo>(),
            Substitute.For<IFileInfo>(),
            Substitute.For<IFileInfo>()
        };

        testFileInfoList[0].Name.Returns("trash_2021-05-15_19-00-00");
        testFileInfoList[1].Name.Returns("trash_2021-05-15_20-00-00");
        testFileInfoList[2].Name.Returns("trash_2021-05-15_21-00-00");
        testFileInfoList[3].Name.Returns("trash_2021-05-15_22-00-00");

        fs.DirectoryInfo.FromDirectoryName(Arg.Any<string>()).GetFiles()
            .Returns(testFileInfoList);

        janitor.DeleteOldestLogFiles(2);

        testFileInfoList[0].Received().Delete();
        testFileInfoList[1].Received().Delete();
        testFileInfoList[2].DidNotReceive().Delete();
        testFileInfoList[3].DidNotReceive().Delete();
    }
}
