using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Threading.Tasks;
using LibGit2Sharp;
using TrashLib.Config;

namespace TrashLib.Radarr.CustomFormat.Guide
{
    internal class LocalRepoCustomFormatJsonParser : IRadarrGuideService
    {
        private readonly IFileSystem _fileSystem;
        private readonly string _repoPath;

        public LocalRepoCustomFormatJsonParser(IFileSystem fileSystem, IResourcePaths paths)
        {
            _fileSystem = fileSystem;
            _repoPath = paths.RepoPath;
        }

        public async Task<IEnumerable<string>> GetCustomFormatJson()
        {
            await Task.Run(() =>
            {
                if (!Repository.IsValid(_repoPath))
                {
                    _fileSystem.Directory.Delete(_repoPath, true);
                    Repository.Clone("https://github.com/TRaSH-/Guides.git", _repoPath, new CloneOptions
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
                Commands.Fetch(repo, origin.Name, origin.FetchRefSpecs.Select(s => s.Specification), null, "");

                repo.Reset(ResetMode.Hard, repo.Branches["origin/master"].Tip);
            });

            var jsonDir = Path.Combine(_repoPath, "docs/json/radarr");
            var tasks = _fileSystem.Directory.GetFiles(jsonDir, "*.json")
                .Select(async f => await _fileSystem.File.ReadAllTextAsync(f));

            return await Task.WhenAll(tasks);
        }
    }
}
