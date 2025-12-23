using Libs.Data.Models;
using Libs.Services.ScannerStrategies;
using System;
using System.IO;
using System.Threading;

namespace Libs.Services
{
    /// <summary>
    /// Represents an active file system watcher session for a specific roll
    /// </summary>
    public class WatcherSession
    {
        public Guid SessionId { get; set; } = Guid.NewGuid();

        public Guid RollId { get; set; }

        public required Roll Roll { get; set; }

        public required Scanner Scanner { get; set; }

        public required IScannerStrategy Strategy { get; set; }

        public required FileSystemWatcher Watcher { get; set; }

        public DateTime StartedAt { get; set; } = DateTime.UtcNow;

        public CancellationTokenSource? CancellationToken { get; set; }

        // For Noritsu scanners: inner file watcher and timer
        public FileSystemWatcher? InnerFileWatcher { get; set; }
        public System.Timers.Timer? CompletionTimer { get; set; }
    }
}
