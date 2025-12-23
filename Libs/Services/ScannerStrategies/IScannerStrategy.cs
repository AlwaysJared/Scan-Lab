using Libs.Data.Models;
using Libs.Enums;

namespace Libs.Services.ScannerStrategies
{
    public interface IScannerStrategy
    {
        /// <summary>
        /// Returns the actual directory path to watch based on scanner's WatchedDir.
        /// Example: For HS-1800, appends daily folder like "20251219"
        /// </summary>
        string ResolveWatchPath(Scanner scanner);

        /// <summary>
        /// Should the FileSystemWatcher be configured with recursive = true?
        /// </summary>
        bool IsRecursive { get; }

        /// <summary>
        /// How completion is detected for this scanner model
        /// </summary>
        CompletionDetectionMode CompletionMode { get; }

        /// <summary>
        /// Delay in seconds for time-based completion (null if not applicable)
        /// </summary>
        int? CompletionDelaySeconds { get; }

        /// <summary>
        /// Given scanner's WatchedDir, find the newest roll directory.
        /// This replaces the hardcoded logic in RollRepository.ProcessRoll lines 112-122.
        /// </summary>
        Task<string?> GetLatestRollDirectory(Scanner scanner);

        /// <summary>
        /// Called when a new directory is detected (for auto-processing profiles).
        /// Returns true if processing should be triggered automatically.
        /// </summary>
        Task<bool> ShouldAutoProcess(Scanner scanner, string directoryPath);
    }
}
