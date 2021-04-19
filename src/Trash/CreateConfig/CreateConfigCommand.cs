using System.IO.Abstractions;
using System.Threading.Tasks;
using CliFx;
using CliFx.Attributes;
using CliFx.Exceptions;
using CliFx.Infrastructure;
using Common;
using JetBrains.Annotations;
using Serilog;
using Trash.Command;

namespace Trash.CreateConfig
{
    [Command("create-config", Description = "Create a starter YAML configuration file")]
    [UsedImplicitly]
    public class CreateConfigCommand : ICommand
    {
        private readonly IFileSystem _fileSystem;

        public CreateConfigCommand(ILogger logger, IFileSystem fileSystem)
        {
            Log = logger;
            _fileSystem = fileSystem;
        }

        private ILogger Log { get; }

        [CommandOption("path", 'p', Description =
            "Path where the new YAML file should be created. Must include the filename (e.g. path/to/config.yml). " +
            "File must not already exist. If not specified, uses the default path of `trash.yml` right next to the " +
            "executable.")]
        public string Path { get; [UsedImplicitly] set; } = BaseCommand.DefaultConfigPath;

        public ValueTask ExecuteAsync(IConsole console)
        {
            var reader = new ResourceDataReader(typeof(Program));
            var ymlData = reader.ReadData("trash-config-template.yml");

            if (_fileSystem.File.Exists(Path))
            {
                throw new CommandException($"The file {Path} already exists. Please choose another path or " +
                                           "delete/move the existing file and run this command again.");
            }

            _fileSystem.File.WriteAllText(Path, ymlData);
            Log.Information("Created configuration at: {Path}", Path);
            return default;
        }
    }
}
