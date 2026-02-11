using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Libs.Services.SP500Export
{
    /// <summary>
    /// Status of an SP500 export session.
    /// </summary>
    public enum SP500ExportStatus
    {
        Watching,
        Detected,
        Copying,
        Complete,
        Interrupted,
        TimedOut,
        Cancelled,
        Error
    }

    /// <summary>
    /// Manages the lifecycle of a single roll's auto-export from the Frontier network share.
    /// Coordinates InnerPoller + PollingObserver to detect exit file, copy images, and signal completion.
    /// </summary>
    public class SP500ExportSession : IDisposable
    {
        public Guid SessionId { get; } = Guid.NewGuid();
        public Guid RollId { get; }
        public Guid ScannerId { get; }
        public string FrontierSharePath { get; }
        public string StagingPath { get; }
        public string? DetectedRollNumber { get; private set; }
        public SP500ExportStatus Status { get; private set; } = SP500ExportStatus.Watching;
        public DateTime StartedAt { get; } = DateTime.UtcNow;
        public int ImagesExported { get; private set; }

        private readonly string _endOfExportFileName;
        private readonly int _sensitivity;
        private readonly int _maxCopyRetries;
        private readonly int _timeoutMinutes;
        private readonly ILogger? _logger;
        private readonly CancellationTokenSource _cts = new();

        private PollingObserver? _outerObserver;
        private PollingObserver? _innerObserver;
        private System.Timers.Timer? _timeoutTimer;

        // Populated after outer poller detects a roll set
        private OuterPoller? _outerPoller;
        private InnerPoller? _innerPoller;

        /// <summary>
        /// Raised when the session completes (images copied to staging).
        /// The List contains the paths of images in the staging directory.
        /// </summary>
        public event Func<SP500ExportSession, Task>? OnCompleted;

        /// <summary>
        /// Raised when the session fails (timeout, error, etc.).
        /// </summary>
        public event Func<SP500ExportSession, Task>? OnFailed;

        public SP500ExportSession(
            Guid rollId,
            Guid scannerId,
            string frontierSharePath,
            string stagingPath,
            string endOfExportFileName,
            int sensitivity,
            int maxCopyRetries,
            int timeoutMinutes,
            ILogger? logger = null)
        {
            RollId = rollId;
            ScannerId = scannerId;
            FrontierSharePath = frontierSharePath;
            StagingPath = stagingPath;
            _endOfExportFileName = endOfExportFileName;
            _sensitivity = sensitivity;
            _maxCopyRetries = maxCopyRetries;
            _timeoutMinutes = timeoutMinutes;
            _logger = logger;
        }

        /// <summary>
        /// Starts the export session: outer polling → inner polling → copy → completion.
        /// This runs as a background task.
        /// </summary>
        public Task StartAsync()
        {
            _logger?.LogInformation("Starting SP500 export session {SessionId} for roll {RollId}", SessionId, RollId);

            // Start timeout timer
            _timeoutTimer = new System.Timers.Timer(_timeoutMinutes * 60 * 1000);
            _timeoutTimer.Elapsed += OnTimeout;
            _timeoutTimer.AutoReset = false;
            _timeoutTimer.Start();

            // Cleanup stale directories first
            SP500Cleanup.CleanupStaleDirectoriesAsync(FrontierSharePath, _logger);

            // Start outer polling
            _outerPoller = new OuterPoller(FrontierSharePath, _sensitivity, _logger);
            _outerObserver = new PollingObserver(
                TimeSpan.FromSeconds(3),
                _outerPoller.PollAsync,
                _logger);

            _outerObserver.Start();

            // Monitor outer observer completion in background
            _ = Task.Run(async () =>
            {
                try
                {
                    // Wait for outer poller to detect a roll set
                    while (!_outerObserver.IsComplete && !_cts.Token.IsCancellationRequested)
                    {
                        await Task.Delay(500, _cts.Token);
                    }

                    if (_cts.Token.IsCancellationRequested)
                        return;

                    // Roll set detected
                    Status = SP500ExportStatus.Detected;
                    DetectedRollNumber = _outerPoller.RollNumber;
                    _logger?.LogInformation("Roll set detected: {RollNumber}", DetectedRollNumber);

                    // Create staging directory
                    Directory.CreateDirectory(StagingPath);

                    // Start inner polling for exit file
                    _innerPoller = new InnerPoller(
                        _outerPoller.RollDir!,
                        _outerPoller.AcImgConv!,
                        StagingPath,
                        _endOfExportFileName,
                        FrontierSharePath,
                        _outerPoller.CreatedDirectories,
                        _sensitivity,
                        _logger);

                    _innerObserver = new PollingObserver(
                        TimeSpan.FromSeconds(3),
                        _innerPoller.PollAsync,
                        _logger);

                    _innerObserver.Start();

                    // Wait for inner poller to complete
                    while (!_innerObserver.IsComplete && !_cts.Token.IsCancellationRequested)
                    {
                        await Task.Delay(500, _cts.Token);
                    }

                    if (_cts.Token.IsCancellationRequested)
                        return;

                    // Inner poller completed — handle result
                    await HandleInnerPollerResult();
                }
                catch (OperationCanceledException)
                {
                    // Expected on cancellation
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error in export session {SessionId}", SessionId);
                    Status = SP500ExportStatus.Error;
                    OnFailed?.Invoke(this);
                }
            }, _cts.Token);

            return Task.CompletedTask;
        }

        private async Task HandleInnerPollerResult()
        {
            if (_innerPoller == null || _outerPoller == null)
                return;

            _timeoutTimer?.Stop();

            if (_innerPoller.IsNewSet && _innerPoller.ImagesDetected.Count == 0)
            {
                // Interrupted with no images — just cleanup
                _logger?.LogWarning("Export interrupted (new roll) with no images for session {SessionId}", SessionId);
                Status = SP500ExportStatus.Interrupted;
                await SP500Cleanup.DeleteDirectoriesAsync(_outerPoller.CreatedDirectories, _logger);
                CleanupStagingDirectory();
                OnFailed?.Invoke(this);
                return;
            }

            // Copy images to staging directory
            Status = SP500ExportStatus.Copying;
            _logger?.LogInformation("Copying {Count} images for session {SessionId}",
                _innerPoller.ImagesDetected.Count, SessionId);

            var copier = new ImageCopier(
                _innerPoller.ImagesDetected,
                StagingPath,
                _maxCopyRetries,
                _logger);

            bool copySuccess = await copier.CopyImagesAsync(_cts.Token);

            if (copySuccess)
            {
                ImagesExported = copier.ImagesCopied.Count;
                Status = SP500ExportStatus.Complete;
                _logger?.LogInformation("Export session {SessionId} completed: {Count} images",
                    SessionId, ImagesExported);

                // Cleanup Frontier source directories
                await SP500Cleanup.DeleteDirectoriesAsync(_outerPoller.CreatedDirectories, _logger);

                // Signal completion
                if (OnCompleted != null)
                    await OnCompleted(this);
            }
            else
            {
                _logger?.LogError("Image copy failed for session {SessionId}", SessionId);
                Status = SP500ExportStatus.Error;
                if (OnFailed != null)
                    await OnFailed(this);
            }
        }

        private void OnTimeout(object? sender, System.Timers.ElapsedEventArgs e)
        {
            _logger?.LogError("Export session {SessionId} timed out after {Minutes} minutes", SessionId, _timeoutMinutes);
            Status = SP500ExportStatus.TimedOut;

            Cancel();

            // Cleanup Frontier source directories if any were detected
            if (_outerPoller?.CreatedDirectories != null)
                SP500Cleanup.DeleteDirectoriesAsync(_outerPoller.CreatedDirectories, _logger);

            CleanupStagingDirectory();
            OnFailed?.Invoke(this);
        }

        /// <summary>
        /// Cancels the session gracefully.
        /// </summary>
        public void Cancel()
        {
            if (Status == SP500ExportStatus.Complete || Status == SP500ExportStatus.Cancelled)
                return;

            _logger?.LogInformation("Cancelling export session {SessionId}", SessionId);
            Status = SP500ExportStatus.Cancelled;

            _timeoutTimer?.Stop();
            _outerObserver?.Stop();
            _innerObserver?.Stop();
            _cts.Cancel();

            // Cleanup source and staging directories
            if (_outerPoller?.CreatedDirectories != null)
                SP500Cleanup.DeleteDirectoriesAsync(_outerPoller.CreatedDirectories, _logger);

            CleanupStagingDirectory();
        }

        private void CleanupStagingDirectory()
        {
            try
            {
                if (Directory.Exists(StagingPath))
                {
                    Directory.Delete(StagingPath, true);
                    _logger?.LogDebug("Deleted staging directory: {Path}", StagingPath);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error deleting staging directory {Path}", StagingPath);
            }
        }

        public void Dispose()
        {
            Cancel();
            _timeoutTimer?.Dispose();
            _outerObserver?.Dispose();
            _innerObserver?.Dispose();
            _cts.Dispose();
        }
    }
}
