using System.IO;
using System.IO.Abstractions;
using Newtonsoft.Json;

namespace Recyclarr.Code.Settings
{
    public class SettingsPersister : ISettingsPersister
    {
        private readonly IFileSystem _fileSystem;

        public SettingsPersister(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

        public T LoadSettings<T>(string jsonFile)
            where T : new()
        {
            var filePath = Path.Combine(AppPaths.SettingsDirectory, jsonFile);

            // ReSharper disable once ConvertIfStatementToReturnStatement
            if (_fileSystem.File.Exists(filePath))
            {
                return JsonConvert.DeserializeObject<T>(_fileSystem.File.ReadAllText(filePath));
            }

            // Create with defaults
            return new T();
        }

        public void SaveSettings<T>(string jsonFile, T settingsObject)
        {
            var file = _fileSystem.FileInfo.FromFileName(Path.Combine(AppPaths.SettingsDirectory, jsonFile));
            file.Directory.Create(); // create directories if they do not exist
            _fileSystem.File.WriteAllText(file.FullName,
                JsonConvert.SerializeObject(settingsObject, Formatting.Indented));
        }
    }
}
