using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using System.Reactive.Linq;
using Flurl.Http;

namespace Recyclarr.Cli.Tests.Reusable;

internal class RemoteRepoFileMapper
{
    private Dictionary<string, string>? _guideDataCache;

    [SuppressMessage("Design", "CA1054:URI-like parameters should not be strings")]
    public async Task DownloadFiles(string urlPrefix, params string[] repoFilePaths)
    {
        var dictionary = await repoFilePaths
            .ToObservable()
            .Select(x =>
                Observable.FromAsync(async ct =>
                {
                    var content = await $"{urlPrefix}/{x}".GetStringAsync(cancellationToken: ct);
                    return (file: x, content);
                })
            )
            .Merge(8)
            .ToList();

        _guideDataCache = dictionary.ToDictionary(x => x.file, x => x.content);
    }

    public void AddToFilesystem(MockFileSystem fs, IDirectoryInfo containingDir)
    {
        ArgumentNullException.ThrowIfNull(_guideDataCache);

        foreach (var (file, content) in _guideDataCache)
        {
            fs.AddFile(containingDir.File(file), new MockFileData(content));
        }
    }
}
