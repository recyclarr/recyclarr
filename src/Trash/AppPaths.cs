using System;
using System.IO;

namespace Trash
{
    internal static class AppPaths
    {
        public static string AppDataPath { get; } =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "trash-updater");

        public static string DefaultConfigPath { get; } = Path.Combine(AppContext.BaseDirectory, "trash.yml");

        public static string LogDirectory { get; } = Path.Combine(AppDataPath, "logs");

        public static string RepoDirectory { get; } = Path.Combine(AppDataPath, "repo");
    }
}
