using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Threading.Tasks;
using LibGit2Sharp;
using Serilog;
using TrashLib.Config.Settings;
using TrashLib.Radarr.Config;

namespace TrashLib.Radarr.CustomFormat.Guide
{
    internal class LocalRepoCustomFormatJsonParser : IRadarrGuideService
    {
        private readonly IFileSystem _fileSystem;
        private readonly ISettingsProvider _settings;
        private readonly ILogger _log;
        private readonly string _repoPath;

        public LocalRepoCustomFormatJsonParser(
            IFileSystem fileSystem,
            IResourcePaths paths,
            ISettingsProvider settings,
            ILogger log)
        {
            _fileSystem = fileSystem;
            _settings = settings;
            _log = log;
            _repoPath = paths.RepoPath;
        }

        public async Task<IEnumerable<string>> GetCustomFormatJsonAsync()
        {
            CloneOrUpdateGitRepo();

            var jsonDir = Path.Combine(_repoPath, "docs/json/radarr");
            var tasks = _fileSystem.Directory.GetFiles(jsonDir, "*.json")
                .Select(async f => await _fileSystem.File.ReadAllTextAsync(f));

            return await Task.WhenAll(tasks);
        }

        private void CloneOrUpdateGitRepo()
        {
            var cloneUrl = _settings.Settings.Repository.CloneUrl;

            if (!Repository.IsValid(_repoPath))
            {
                if (_fileSystem.Directory.Exists(_repoPath))
                {
                    _fileSystem.Directory.Delete(_repoPath, true);
                }

                Repository.Clone(cloneUrl, _repoPath, new CloneOptions
                {
                    RecurseSubmodules = false
                });
            }

            using var repo = new Repository(_repoPath);
            Commands.Checkout(repo, "master", new CheckoutOptions
            {
                CheckoutModifiers = CheckoutModifiers.Force
            });

            var origin = repo.Network.Remotes["origin"];
            if (origin.Url != cloneUrl)
            {
                _log.Debug(
                    "Origin's URL ({OriginUrl}) does not match the clone URL from settings ({CloneUrl}) and will be updated",
                    origin.Url, cloneUrl);

                repo.Network.Remotes.Update("origin", updater => updater.Url = cloneUrl);
                origin = repo.Network.Remotes["origin"];
            }

            Commands.Fetch(repo, origin.Name, origin.FetchRefSpecs.Select(s => s.Specification), null, "");

            repo.Reset(ResetMode.Hard, repo.Branches["origin/master"].Tip);
        }
    }
}
