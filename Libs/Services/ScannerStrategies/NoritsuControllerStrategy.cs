using Libs.Data.Models;
using Libs.Enums;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Libs.Services.ScannerStrategies
{
    /// <summary>
    /// Strategy for Noritsu Controller-based scanners (HS-1800, LS-600, etc.)
    /// Implements smart file watching with timer reset on each new file
    /// </summary>
    public class NoritsuControllerStrategy : IScannerStrategy
    {
        public string ResolveWatchPath(Scanner scanner)
        {
            // Create daily folder path: {WatchedDir}/YYYYMMDD
            var today = DateTime.Now.ToString("yyyyMMdd");
            var dailyPath = Path.Combine(scanner.WatchedDir, today);

            // Ensure daily folder exists
            if (!Directory.Exists(dailyPath))
            {
                Directory.CreateDirectory(dailyPath);
            }

            return dailyPath;
        }

        public bool IsRecursive => true; // Watch subdirectories within daily folder

        public CompletionDetectionMode CompletionMode => CompletionDetectionMode.TimeBasedDelay;

        // Read delay from scanner instance, default to 25 seconds
        public int? CompletionDelaySeconds => null; // Handled per-scanner in ShouldAutoProcess

        public async Task<string?> GetLatestRollDirectory(Scanner scanner)
        {
            var watchPath = ResolveWatchPath(scanner);

            if (!Directory.Exists(watchPath))
                return null;

            var rollDirs = Directory.GetDirectories(watchPath)
                .Select(dir => new
                {
                    Path = dir,
                    WriteTime = Directory.GetLastWriteTimeUtc(dir)
                })
                .OrderByDescending(dir => dir.WriteTime)
                .ToList();

            return await Task.FromResult(rollDirs.FirstOrDefault()?.Path);
        }

        /// <summary>
        /// Smart file watching: Watch for files inside directory, restart timer on each new file
        /// </summary>
        public async Task<bool> ShouldAutoProcess(Scanner scanner, string directoryPath)
        {
            // NOTE: This method is called by FileSystemWatcherService which handles
            // the actual file watching and timer logic. This implementation is a placeholder.
            // The real logic is in FileSystemWatcherService.OnDirectoryCreated()

            // Verify directory exists
            if (!Directory.Exists(directoryPath))
                return false;

            // Get delay from scanner instance, default to 25 seconds
            var delaySeconds = scanner.AutoProcessDelaySeconds ?? 25;

            // Simple delay for now - FileSystemWatcherService will handle smart watching
            await Task.Delay(TimeSpan.FromSeconds(delaySeconds));

            // Verify directory still exists and has files
            if (!Directory.Exists(directoryPath))
                return false;

            var files = Directory.GetFiles(directoryPath);
            return files.Length > 0;
        }
    }
}
