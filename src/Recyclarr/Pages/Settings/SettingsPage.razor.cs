#nullable disable
using System.Collections.Generic;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Components;
using Recyclarr.Code;
using Recyclarr.Code.Settings;
using Recyclarr.Code.Settings.Persisters;

namespace Recyclarr.Pages.Settings
{
    [UsedImplicitly]
    public partial class SettingsPage
    {
        private readonly HashSet<IValueWatcher> _changedValues = new();
        private ValueWatcher<string> _repoPath;
        private AppSettings _settings;

        [Inject]
        public IAppSettingsPersister Persister { get; set; }

        protected override void OnInitialized()
        {
            base.OnInitialized();
            LoadSettings();
        }

        private void LoadSettings()
        {
            _settings = Persister.Load();
            SetupWatcher(_repoPath = new ValueWatcher<string>(() => _settings.RepoPath, v => _settings.RepoPath = v));
        }

        private void SetupWatcher(IValueWatcher watcher)
        {
            watcher.Changed += OnValueChanged;
        }

        private void OnValueChanged(object obj, bool isSame)
        {
            var watcher = (IValueWatcher) obj;

            if (!isSame)
            {
                _changedValues.Add(watcher);
            }
            else
            {
                _changedValues.Remove(watcher);
            }
        }

        private void SaveChanges()
        {
            Persister.Save(_settings);
            LoadSettings();
        }

        private void DiscardChanges()
        {
            foreach (var value in _changedValues)
            {
                value.Revert();
            }
        }

        private void ResetDefaults()
        {
            _settings = new AppSettings();
            SaveChanges();
        }
    }
}
