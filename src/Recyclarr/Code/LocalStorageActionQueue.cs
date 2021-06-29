using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Blazored.LocalStorage;

namespace Recyclarr.Code
{
    public interface ILocalStorageActionQueue
    {
        void Load<T>(string settingName, Func<T?, Task> assignmentFunc);
        void Save<T>(string settingName, T? value);
        Task Process();
    }

    public class LocalStorageActionQueue : ILocalStorageActionQueue
    {
        private readonly ILocalStorageService _localStorage;
        private readonly Queue<Func<Task>> _queue = new();

        public LocalStorageActionQueue(ILocalStorageService localStorage)
        {
            _localStorage = localStorage;
        }

        public void Load<T>(string settingName, Func<T?, Task> assignmentFunc)
        {
            _queue.Enqueue(async () => await assignmentFunc(await _localStorage.GetItemAsync<T>(settingName)));
        }

        public void Save<T>(string settingName, T? value)
        {
            _queue.Enqueue(async () => await _localStorage.SetItemAsync(settingName, value));
        }

        public async Task Process()
        {
            while (_queue.TryDequeue(out var action))
            {
                await action.Invoke();
            }
        }
    }
}
