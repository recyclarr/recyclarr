using System.IO.Abstractions;
using Microsoft.Extensions.Configuration;
using Recyclarr.Common;
using Recyclarr.Common.Extensions;

namespace Recyclarr.Settings.Configuration;

public static class SettingsConfiguration
{
    /// <summary>
    /// The configuration section that holds <c>settings.yml</c>. Only that file feeds it: hosts
    /// keep environment variables and command-line arguments out of this section.
    /// </summary>
    public const string SectionName = "RecyclarrSettings";

    extension(IConfigurationBuilder builder)
    {
        /// <summary>
        /// Adds <c>settings.yml</c> (or <c>settings.yaml</c>) from the configuration directory and
        /// returns the report its provider fills when the configuration loads. The file is read
        /// through the directory's filesystem; <c>!env_var</c> tags read
        /// <paramref name="environment"/>. A missing file adds no keys; problems never throw and
        /// are reported instead.
        /// </summary>
        public SettingsLoadReport AddSettingsYaml(
            IDirectoryInfo configDirectory,
            IConfiguration environment
        )
        {
            var report = new SettingsLoadReport();

            IFileInfo? file;
            try
            {
                file = configDirectory.YamlFile("settings");
            }
            catch (ConflictingYamlFilesException e)
            {
                report.Replace([], [report.Error(e.Message)]);
                return report;
            }

            report.FileName = file?.Name ?? "settings.yml";
            builder.Add(
                new SettingsYamlConfigurationSource
                {
                    Report = report,
                    Tags = new SettingsYamlTags(environment, configDirectory),
                    FileProvider = new FileSystemFileProvider(configDirectory),
                    Path = report.FileName,
                    Optional = true,
                }
            );

            return report;
        }
    }
}
