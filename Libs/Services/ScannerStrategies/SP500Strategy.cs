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
        public virtual string ResolveWatchPath(Scanner scanner)
        {
            // SP-500 doesn't use daily folders - watch directory directly
            return scanner.WatchedDir;
        }

        public virtual bool IsRecursive => false; // Only watch top level

        public virtual CompletionDetectionMode CompletionMode => CompletionDetectionMode.Manual;

        public virtual int? CompletionDelaySeconds => null; // Manual triggering only

        public virtual async Task<string?> GetLatestRollDirectory(Scanner scanner)
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

        public virtual async Task<bool> ShouldAutoProcess(Scanner scanner, string directoryPath)
        {
            // Manual strategy never auto-processes
            return await Task.FromResult(false);
        }
    }
}
