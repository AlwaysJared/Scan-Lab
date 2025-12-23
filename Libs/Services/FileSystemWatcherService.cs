using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Libs.Data.Models;
using Libs.Enums;
using Libs.Services.ScannerStrategies;
using Microsoft.Extensions.Logging;

namespace Libs.Services
{
    public class FileSystemWatcherService : IDisposable
    {
        private readonly ConcurrentDictionary<Guid, WatcherSession> _activeSessions = new();
        private readonly ILogger<FileSystemWatcherService>? _logger;

        // Delegate for auto-processing callback
        public delegate Task ProcessRollDelegate(Guid rollId, Guid? staffId);
        public ProcessRollDelegate? OnAutoProcessRoll { get; set; }

        public FileSystemWatcherService(ILogger<FileSystemWatcherService>? logger = null)
        {
            _logger = logger;
        }

        /// <summary>
        /// Starts a file system watcher for a specific roll based on its scanner's profile
        /// </summary>
        public Guid? StartWatcherForRoll(Roll roll, Guid? staffId)
        {
            if (roll.Order?.Scanner?.Profile == null)
            {
                _logger?.LogError($"Cannot start watcher: Roll {roll.RollId} has no scanner profile");
                return null;
            }

            var strategy = ScannerStrategyFactory.CreateStrategy(roll.Order.Scanner);
            if (strategy == null)
            {
                _logger?.LogError($"Cannot start watcher: Invalid strategy for scanner {roll.Order.Scanner.ScannerName}");
                return null;
            }

            // Prevent multiple watchers on same scanner
            var existingSessionOnScanner = _activeSessions.Values
                .FirstOrDefault(s => s.Scanner.Id == roll.Order.Scanner.Id);

            if (existingSessionOnScanner != null)
            {
                _logger?.LogWarning($"Scanner {roll.Order.Scanner.ScannerName} already has active watcher for roll {existingSessionOnScanner.RollId}");
                return null;
            }

            // Check if watcher already exists for this roll
            var existingSession = _activeSessions.Values.FirstOrDefault(s => s.RollId == roll.RollId);
            if (existingSession != null)
            {
                _logger?.LogWarning($"Watcher already active for roll {roll.RollId}");
                return existingSession.SessionId;
            }

            try
            {
                var watchPath = strategy.ResolveWatchPath(roll.Order.Scanner);

                if (!Directory.Exists(watchPath))
                {
                    _logger?.LogError($"Watch path does not exist: {watchPath}");
                    return null;
                }

                var watcher = new FileSystemWatcher(watchPath)
                {
                    NotifyFilter = NotifyFilters.DirectoryName | NotifyFilters.LastWrite,
                    IncludeSubdirectories = strategy.IsRecursive
                };

                var session = new WatcherSession
                {
                    RollId = roll.RollId,
                    Roll = roll,
                    Scanner = roll.Order.Scanner,
                    Strategy = strategy,
                    Watcher = watcher,
                    CancellationToken = new CancellationTokenSource()
                };

                // Attach event handlers
                watcher.Created += async (sender, e) => await OnDirectoryCreated(session, e, staffId);

                watcher.EnableRaisingEvents = true;

                _activeSessions[session.SessionId] = session;

                _logger?.LogInformation($"Started watcher for roll {roll.RollId} on scanner {roll.Order.Scanner.ScannerName} (Session: {session.SessionId})");

                return session.SessionId;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Failed to start watcher for roll {roll.RollId}");
                return null;
            }
        }

        /// <summary>
        /// Stops the watcher for a specific roll
        /// </summary>
        public bool StopWatcherForRoll(Guid rollId)
        {
            var session = _activeSessions.Values.FirstOrDefault(s => s.RollId == rollId);

            if (session == null)
            {
                _logger?.LogWarning($"No active watcher found for roll {rollId}");
                return false;
            }

            return StopWatcher(session.SessionId);
        }

        /// <summary>
        /// Stops a watcher by session ID
        /// </summary>
        public bool StopWatcher(Guid sessionId)
        {
            if (!_activeSessions.TryRemove(sessionId, out var session))
            {
                _logger?.LogWarning($"Session {sessionId} not found");
                return false;
            }

            try
            {
                session.CancellationToken?.Cancel();

                // Dispose inner file watcher if exists (Noritsu scanners)
                if (session.InnerFileWatcher != null)
                {
                    session.InnerFileWatcher.EnableRaisingEvents = false;
                    session.InnerFileWatcher.Dispose();
                }

                // Dispose timer if exists
                if (session.CompletionTimer != null)
                {
                    session.CompletionTimer.Stop();
                    session.CompletionTimer.Dispose();
                }

                // Dispose outer watcher
                session.Watcher.EnableRaisingEvents = false;
                session.Watcher.Dispose();
                session.CancellationToken?.Dispose();

                _logger?.LogInformation($"Stopped watcher session {sessionId} for roll {session.RollId}");

                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Error stopping watcher session {sessionId}");
                return false;
            }
        }

        /// <summary>
        /// Gets all active watcher sessions
        /// </summary>
        public IEnumerable<WatcherSession> GetActiveSessions()
        {
            return _activeSessions.Values;
        }

        /// <summary>
        /// Checks if a watcher is active for a specific roll
        /// </summary>
        public bool IsWatcherActive(Guid rollId)
        {
            return _activeSessions.Values.Any(s => s.RollId == rollId);
        }

        /// <summary>
        /// Event handler when a directory is created
        /// </summary>
        private async Task OnDirectoryCreated(WatcherSession session, FileSystemEventArgs e, Guid? staffId)
        {
            if (!e.ChangeType.HasFlag(WatcherChangeTypes.Created) || !Directory.Exists(e.FullPath))
                return;

            _logger?.LogInformation($"Directory created: {e.FullPath} (Session: {session.SessionId})");

            try
            {
                // For Noritsu scanners: implement smart file watching
                if (session.Strategy is NoritsuControllerStrategy)
                {
                    await HandleNoritsuDirectory(session, e.FullPath, staffId);
                }
                else
                {
                    // For other strategies: use simple delay from strategy
                    var shouldProcess = await session.Strategy.ShouldAutoProcess(session.Scanner, e.FullPath);

                    if (shouldProcess && OnAutoProcessRoll != null)
                    {
                        _logger?.LogInformation($"Triggering auto-process for roll {session.RollId}");
                        await OnAutoProcessRoll(session.RollId, staffId);
                        StopWatcher(session.SessionId);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Error in OnDirectoryCreated handler for session {session.SessionId}");
                // Stop watcher on error, require manual completion
                StopWatcher(session.SessionId);
            }
        }

        /// <summary>
        /// Handle Noritsu scanner directory with smart file watching
        /// </summary>
        private async Task HandleNoritsuDirectory(WatcherSession session, string directoryPath, Guid? staffId)
        {
            var delay = session.Scanner.AutoProcessDelaySeconds ?? 25;

            _logger?.LogInformation($"Starting smart file watching for Noritsu scanner in {directoryPath} with {delay}s delay");

            // Create inner file watcher for files inside the directory
            var fileWatcher = new FileSystemWatcher(directoryPath)
            {
                NotifyFilter = NotifyFilters.FileName,
                IncludeSubdirectories = false
            };

            // Create timer
            var timer = new System.Timers.Timer(delay * 1000);
            var fileCount = 0;

            // Store in session for cleanup
            session.InnerFileWatcher = fileWatcher;
            session.CompletionTimer = timer;

            // Reset timer on each new file
            fileWatcher.Created += (sender, e) =>
            {
                fileCount++;
                _logger?.LogDebug($"File created: {e.Name} (Total: {fileCount})");
                timer.Stop();
                timer.Start();
            };

            // When timer expires (no new files for delay seconds)
            timer.Elapsed += async (sender, e) =>
            {
                timer.Stop();
                fileWatcher.EnableRaisingEvents = false;

                _logger?.LogInformation($"Timer expired. {fileCount} files detected. Triggering auto-process for roll {session.RollId}");

                try
                {
                    // Verify directory still exists and has files
                    if (Directory.Exists(directoryPath) && Directory.GetFiles(directoryPath).Length > 0)
                    {
                        if (OnAutoProcessRoll != null)
                        {
                            await OnAutoProcessRoll(session.RollId, staffId);
                        }
                    }
                    else
                    {
                        _logger?.LogWarning($"Directory {directoryPath} is empty or doesn't exist");
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, $"Error during auto-processing for roll {session.RollId}");
                }
                finally
                {
                    // Stop watcher session
                    StopWatcher(session.SessionId);
                }
            };

            // Start watching
            fileWatcher.EnableRaisingEvents = true;
            timer.Start();

            _logger?.LogInformation($"Smart file watching active for session {session.SessionId}");

            // Keep task alive until timer completes
            await Task.CompletedTask;
        }

        /// <summary>
        /// Dispose all active watchers
        /// </summary>
        public void Dispose()
        {
            foreach (var session in _activeSessions.Values)
            {
                session.CancellationToken?.Cancel();

                // Dispose inner watcher if exists
                if (session.InnerFileWatcher != null)
                {
                    session.InnerFileWatcher.EnableRaisingEvents = false;
                    session.InnerFileWatcher.Dispose();
                }

                // Dispose timer if exists
                if (session.CompletionTimer != null)
                {
                    session.CompletionTimer.Stop();
                    session.CompletionTimer.Dispose();
                }

                // Dispose outer watcher
                session.Watcher.EnableRaisingEvents = false;
                session.Watcher.Dispose();
                session.CancellationToken?.Dispose();
            }

            _activeSessions.Clear();

            _logger?.LogInformation("FileSystemWatcherService disposed");
        }
    }
}