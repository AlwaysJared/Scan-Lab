using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;

namespace Libs.Services
{
    public class FileSystemWatcherService : IDisposable
    {
        private readonly ConcurrentDictionary<Guid, FileSystemWatcher> _watchers = new();

        public Guid CreateWatcher(string path)
        {
            if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
            {
                throw new ArgumentException("Invalid directory path.");
            }

            var id = Guid.NewGuid();
            var watcher = new FileSystemWatcher(path)
            {
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite
            };

            watcher.Changed += OnChanged;
            watcher.Created += OnChanged;
            watcher.Deleted += OnChanged;
            watcher.Renamed += OnRenamed;

            watcher.EnableRaisingEvents = true;

            _watchers[id] = watcher;
            return id;
        }

        private void OnChanged(object sender, FileSystemEventArgs e)
        {
            Console.WriteLine($"File {e.FullPath} was {e.ChangeType}");
        }

        private void OnRenamed(object sender, RenamedEventArgs e)
        {
            Console.WriteLine($"File {e.OldFullPath} was renamed to {e.FullPath}");
        }

        public void DisposeWatcher(Guid id)
        {
            if (_watchers.TryRemove(id, out var watcher))
            {
                watcher.EnableRaisingEvents = false;
                watcher.Dispose();
            }
        }

        public void Dispose()
        {
            foreach (var watcher in _watchers.Values)
            {
                watcher.Dispose();
            }
            _watchers.Clear();
        }
    }

}