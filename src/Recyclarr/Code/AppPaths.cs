using System;
using System.IO;

namespace Recyclarr.Code
{
    public static class AppPaths
    {
        public static string DataDirectory { get; } =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "recyclarr");

        public static string SettingsDirectory { get; } = Path.Combine(DataDirectory, "settings");
        public static string DefaultRepoPath { get; } = Path.Combine(DataDirectory, "repo");
    }
}
