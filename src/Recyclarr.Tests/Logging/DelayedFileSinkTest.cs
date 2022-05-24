using System.IO.Abstractions;
using AutoFixture.NUnit3;
using NSubstitute;
using NUnit.Framework;
using Recyclarr.Logging;
using Serilog.Events;
using TestLibrary.AutoFixture;
using TrashLib;

namespace Recyclarr.Tests.Logging;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class DelayedFileSinkTest
{
    [Test, AutoMockData]
    public void Should_not_open_file_if_app_data_invalid(
        [Frozen] IAppPaths paths,
        [Frozen] IFileSystem fs,
        LogEvent logEvent,
        DelayedFileSink sut)
    {
        paths.IsAppDataPathValid.Returns(false);

        sut.Emit(logEvent);

        fs.File.DidNotReceiveWithAnyArgs().OpenWrite(default!);
    }
}
