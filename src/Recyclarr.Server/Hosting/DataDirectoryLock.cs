using System.IO.Abstractions;
using Recyclarr.Platform;

namespace Recyclarr.Server.Hosting;

/// <summary>
/// Gives one server process exclusive use of the data and state directories. Two servers that
/// share either directory race on the resource cache, state files, the server database, and log
/// files; an ephemeral server launched by the CLI next to a running server is the common case.
/// </summary>
/// <remarks>
/// The lock is an open handle on a file with no sharing, held until the process exits. The OS
/// drops it when the process dies, so a crash never leaves a lock that blocks the next start; the
/// lock file itself stays behind and is reused. On Linux and macOS the lock is advisory (flock):
/// it excludes other Recyclarr servers, not other programs, and network file systems may not
/// honor it.
/// </remarks>
internal sealed class DataDirectoryLock(IAppPaths paths, ServerMode mode) : IDisposable
{
    private const string LockFileName = "recyclarr-server.lock";

    private readonly List<FileSystemStream> _handles = [];

    /// <summary>
    /// Locks each directory, or throws <see cref="FatalException"/> when one cannot be locked,
    /// usually because another server holds it.
    /// </summary>
    public void Acquire()
    {
        // RECYCLARR_DATA_DIR and RECYCLARR_STATE_DIR may name the same directory. Its second
        // handle would collide with the first, as if another server held it.
        var directories = new[] { paths.DataDirectory, paths.StateDirectory }.DistinctBy(d =>
            d.FileSystem.Path.TrimEndingDirectorySeparator(d.FullName)
        );

        foreach (var directory in directories)
        {
            _handles.Add(Lock(directory));
        }
    }

    private FileSystemStream Lock(IDirectoryInfo directory)
    {
        try
        {
            return directory
                .File(LockFileName)
                .Open(FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        }
        catch (IOException e)
        {
            throw new FatalException(LockFailedMessage(directory, e), e);
        }
    }

    // The OS reason is included because IOException also covers failures other than a held lock,
    // and telling them apart needs platform-specific error codes.
    private string LockFailedMessage(IDirectoryInfo directory, IOException e)
    {
        var message =
            $"Cannot lock {directory.FullName}: {e.Message} "
            + "Another Recyclarr server may be using it.";
        return mode == ServerMode.Ephemeral
            ? $"{message} To sync through that server, set server.base_url in cli.yml to its "
                + "address, or stop it."
            : $"{message} Stop that server, or give this one its own directories with "
                + "RECYCLARR_DATA_DIR and RECYCLARR_STATE_DIR.";
    }

    public void Dispose()
    {
        foreach (var handle in _handles)
        {
            handle.Dispose();
        }
    }
}
