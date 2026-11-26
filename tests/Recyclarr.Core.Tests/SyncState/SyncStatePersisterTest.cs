using System.IO.Abstractions;
using NSubstitute.ExceptionExtensions;
using Recyclarr.SyncState;

namespace Recyclarr.Core.Tests.SyncState;

internal sealed class TestSyncStatePersister(ILogger log, ISyncStateStoragePath storagePath)
    : SyncStatePersister(log, storagePath, "test-state")
{
    protected override string DisplayName => "Test State";
}

internal sealed class SyncStatePersisterTest
{
    private readonly MockFileSystem _fs = new();
    private readonly ISyncStateStoragePath _storage = Substitute.For<ISyncStateStoragePath>();

    private TestSyncStatePersister CreateSut() => new(Substitute.For<ILogger>(), _storage);

    [Test]
    public void Load_returns_empty_when_file_does_not_exist()
    {
        _storage.CalculatePath("test-state").Returns(_fs.FileInfo.New("stateFile.json"));
        var sut = CreateSut();

        var result = sut.Load();
        result.Mappings.Should().BeEmpty();
    }

    [Test]
    public void Load_returns_empty_when_json_has_error()
    {
        var sut = CreateSut();
        const string stateJson = """
            {\
              mappings: Hello
            }/
            """;

        _fs.AddFile("stateFile.json", new MockFileData(stateJson));
        _storage.CalculatePath("test-state").Returns(_fs.FileInfo.New("stateFile.json"));

        var result = sut.Load();

        result.Mappings.Should().BeEmpty();
    }

    [Test]
    public void Load_returns_state_data()
    {
        var sut = CreateSut();
        const string stateJson = """
            {
              "mappings": [
                { "trash_id": "abc", "name": "Test", "service_id": 42 }
              ]
            }
            """;

        _fs.AddFile("stateFile.json", new MockFileData(stateJson));
        _storage.CalculatePath("test-state").Returns(_fs.FileInfo.New("stateFile.json"));

        var result = sut.Load();

        result.Mappings.Should().BeEquivalentTo([NewSyncState.Mapping("abc", "Test", 42)]);
    }

    [Test]
    public void Load_classifies_storage_access_failure()
    {
        var file = Substitute.For<IFileInfo>();
        file.Exists.Returns(true);
        file.OpenRead().Throws(new IOException("unavailable"));
        var storage = Substitute.For<ISyncStateStoragePath>();
        storage.CalculatePath("test-state").Returns(file);
        var sut = new TestSyncStatePersister(Substitute.For<ILogger>(), storage);

        var act = sut.Load;

        act.Should()
            .Throw<SyncStateUnavailableException>()
            .WithInnerException<IOException>()
            .WithMessage("unavailable");
    }
}
