using Libs.Data.Models;
using Libs.Enums;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Libs.Services.ScannerStrategies
{
    public class HS1800Strategy : IScannerStrategy
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

        public int? CompletionDelaySeconds => 25; // 25 second delay like FileMover app

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

        public async Task<bool> ShouldAutoProcess(Scanner scanner, string directoryPath)
        {
            // Wait for completion delay
            if (CompletionDelaySeconds.HasValue)
            {
                await Task.Delay(TimeSpan.FromSeconds(CompletionDelaySeconds.Value));
            }

            // Verify directory still exists and has files
            if (!Directory.Exists(directoryPath))
                return false;

            var files = Directory.GetFiles(directoryPath);
            return files.Length > 0;
        }
    }
}
