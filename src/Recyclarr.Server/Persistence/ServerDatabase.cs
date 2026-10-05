using System.IO.Abstractions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Recyclarr.Server.Persistence;

/// <summary>
/// Where the server's database lives. The only place that knows the backend is SQLite; everything
/// else reaches the database through <see cref="ServerDbContext"/>.
/// </summary>
/// <remarks>
/// An in-memory database exists only while a connection to it is open, so this type holds one open
/// for its own lifetime. Register it as a single instance.
/// </remarks>
internal sealed class ServerDatabase : IDisposable
{
    private readonly SqliteConnection? _keepAlive;

    private ServerDatabase(string connectionString, bool keepAlive)
    {
        ConnectionString = connectionString;
        if (keepAlive)
        {
            _keepAlive = new SqliteConnection(connectionString);
            _keepAlive.Open();
        }
    }

    public string ConnectionString { get; }

    public static ServerDatabase File(IFileInfo file)
    {
        var builder = new SqliteConnectionStringBuilder { DataSource = file.FullName };
        return new ServerDatabase(builder.ToString(), keepAlive: false);
    }

    // Each instance gets its own named database, so separate servers in one process (tests) never
    // share state.
    public static ServerDatabase InMemory()
    {
        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = $"recyclarr-{Guid.NewGuid():N}",
            Mode = SqliteOpenMode.Memory,
            Cache = SqliteCacheMode.Shared,
        };
        return new ServerDatabase(builder.ToString(), keepAlive: true);
    }

    // Each context sharing this database keeps its own migration history table, so each migration
    // set has a single owner.
    public void Configure(DbContextOptionsBuilder options, string migrationsHistoryTable)
    {
        options.UseSqlite(
            ConnectionString,
            sqlite => sqlite.MigrationsHistoryTable(migrationsHistoryTable)
        );
    }

    public void Dispose()
    {
        _keepAlive?.Dispose();
    }
}
