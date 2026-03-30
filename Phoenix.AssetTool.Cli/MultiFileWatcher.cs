using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace Phoenix.AssetTool.Cli
{
    public sealed class MultiFileWatcher : IDisposable
    {
        private readonly HashSet<string> _watchedFiles;
        private readonly List<FileSystemWatcher> _watchers = new();
        private readonly Dictionary<string, Timer> _debounceTimers = new();
        private readonly object _lock = new();

        public event Action<string>? FileChanged;

        public MultiFileWatcher(IEnumerable<string> filesToWatch)
        {
            _watchedFiles = new HashSet<string>(
                filesToWatch
                    .Select(Path.GetFullPath)
                    .Select(NormalizePath),
                StringComparer.OrdinalIgnoreCase);

            var directories = _watchedFiles
                .Select(Path.GetDirectoryName)
                .Where(d => !string.IsNullOrWhiteSpace(d))
                .Distinct(StringComparer.OrdinalIgnoreCase)!;

            foreach (var dir in directories)
            {
                var watcher = new FileSystemWatcher(dir!)
                {
                    IncludeSubdirectories = false,
                    NotifyFilter =
                        NotifyFilters.FileName |
                        NotifyFilters.LastWrite |
                        NotifyFilters.CreationTime |
                        NotifyFilters.Size
                };

                watcher.Changed += OnChanged;
                watcher.Created += OnChanged;
                watcher.Renamed += OnRenamed;
                watcher.Deleted += OnChanged;
                watcher.EnableRaisingEvents = true;

                _watchers.Add(watcher);
            }
        }

        private void OnChanged(object sender, FileSystemEventArgs e)
        {
            var fullPath = NormalizePath(Path.GetFullPath(e.FullPath));

            if (_watchedFiles.Contains(fullPath))
                Debounce(fullPath);
        }

        private void OnRenamed(object sender, RenamedEventArgs e)
        {
            var oldPath = NormalizePath(Path.GetFullPath(e.OldFullPath));
            var newPath = NormalizePath(Path.GetFullPath(e.FullPath));

            if (_watchedFiles.Contains(oldPath) || _watchedFiles.Contains(newPath))
                Debounce(newPath);
        }

        private void Debounce(string path, int delayMs = 150)
        {
            lock (_lock)
            {
                if (_debounceTimers.TryGetValue(path, out var existing))
                {
                    existing.Change(delayMs, Timeout.Infinite);
                    return;
                }

                var timer = new Timer(_ =>
                {
                    lock (_lock)
                    {
                        if (_debounceTimers.TryGetValue(path, out var t))
                        {
                            t.Dispose();
                            _debounceTimers.Remove(path);
                        }
                    }
                    FileChanged?.Invoke(path);

                }, null, delayMs, Timeout.Infinite);

                _debounceTimers[path] = timer;
            }
        }

        private static string NormalizePath(string path)
        {
            return path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        public void Dispose()
        {
            foreach (var watcher in _watchers)
            {
                watcher.Changed -= OnChanged;
                watcher.Created -= OnChanged;
                watcher.Renamed -= OnRenamed;
                watcher.Deleted -= OnChanged;
                watcher.Dispose();
            }

            lock (_lock)
            {
                foreach (var timer in _debounceTimers.Values)
                    timer.Dispose();

                _debounceTimers.Clear();
            }

            _watchers.Clear();
        }
    }
}
