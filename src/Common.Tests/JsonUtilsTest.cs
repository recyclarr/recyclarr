using System.IO.Abstractions;
using System.IO.Abstractions.Extensions;
using System.IO.Abstractions.TestingHelpers;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.TestCorrelator;

namespace Common.Tests;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class JsonUtilsTest
{
    [Test]
    public void Log_files_that_do_not_exist()
    {
        using var logContext = TestCorrelator.CreateContext();
        var fs = new MockFileSystem();
        var log = new LoggerConfiguration()
            .MinimumLevel.Is(LogEventLevel.Debug)
            .WriteTo.TestCorrelator()
            .CreateLogger();

        using var context = TestCorrelator.CreateContext();

        var path = fs.CurrentDirectory().SubDirectory("doesnt_exist");

        var result = JsonUtils.GetJsonFilesInDirectories(new[] {path}, log);

        result.Should().BeEmpty();
        TestCorrelator.GetLogEventsFromContextGuid(logContext.Guid)
            .Should().ContainSingle()
            .Which.RenderMessage()
            .Should().Match("*doesnt_exist*");
    }

    [Test]
    public void Log_files_that_only_exist()
    {
        var fs = new MockFileSystem();
        var log = new LoggerConfiguration()
            .MinimumLevel.Is(LogEventLevel.Debug)
            .WriteTo.TestCorrelator()
            .CreateLogger();

        using var logContext = TestCorrelator.CreateContext();

        var path = fs.CurrentDirectory().SubDirectory("exists").File("test.json");
        fs.AddFile(path.FullName, new MockFileData(""));

        var result = JsonUtils.GetJsonFilesInDirectories(new[] {path.Directory}, log);

        result.Should().ContainSingle()
            .Which.FullName
            .Should().Be(path.FullName);

        TestCorrelator.GetLogEventsFromContextGuid(logContext.Guid)
            .Should().BeEmpty();
    }

    [Test]
    public void Log_files_that_both_exist_and_do_not_exist()
    {
        var fs = new MockFileSystem();
        var log = new LoggerConfiguration()
            .MinimumLevel.Is(LogEventLevel.Debug)
            .WriteTo.TestCorrelator()
            .CreateLogger();

        using var logContext = TestCorrelator.CreateContext();

        var paths = new[]
        {
            fs.CurrentDirectory().SubDirectory("does_not_exist"),
            fs.CurrentDirectory().SubDirectory("exists")
        };

        var existingFile = paths[1].File("test.json").FullName;

        fs.AddFile(existingFile, new MockFileData(""));
        paths[1].Refresh();

        var result = JsonUtils.GetJsonFilesInDirectories(paths, log);

        result.Should().ContainSingle()
            .Which.FullName
            .Should().Be(existingFile);

        TestCorrelator.GetLogEventsFromContextGuid(logContext.Guid)
            .Should().ContainSingle()
            .Which.RenderMessage()
            .Should().Match("*does_not_exist*");
    }

    [Test]
    public void Null_paths_are_ignored()
    {
        var result = JsonUtils.GetJsonFilesInDirectories(
            new IDirectoryInfo?[] {null, null},
            Substitute.For<ILogger>());

        result.Should().BeEmpty();
    }
}
