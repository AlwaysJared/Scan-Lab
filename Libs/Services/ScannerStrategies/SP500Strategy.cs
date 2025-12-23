using Libs.Data.Models;
using Libs.Enums;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Libs.Services.ScannerStrategies
{
    public class SP500Strategy : IScannerStrategy
    {
        public string ResolveWatchPath(Scanner scanner)
        {
            // SP-500 doesn't use daily folders - watch directory directly
            return scanner.WatchedDir;
        }

        public bool IsRecursive => false; // Only watch top level

        public CompletionDetectionMode CompletionMode => CompletionDetectionMode.Manual;

        public int? CompletionDelaySeconds => null; // Manual triggering only

        public async Task<string?> GetLatestRollDirectory(Scanner scanner)
        {
            if (!Directory.Exists(scanner.WatchedDir))
                return null;

            var rollDirs = Directory.GetDirectories(scanner.WatchedDir)
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
            // Manual strategy never auto-processes
            return await Task.FromResult(false);
        }
    }
}
