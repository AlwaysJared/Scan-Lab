using Libs.Data.Models;
using Libs.Enums;
using System.IO;
using System.Threading.Tasks;

namespace Libs.Services.ScannerStrategies
{
    /// <summary>
    /// Strategy for SP-500 scanners with automatic export detection via exit file.
    /// The actual polling logic lives in SP500ExporterService — this strategy provides
    /// metadata and path resolution for the system.
    /// </summary>
    public class SP500AutoStrategy : SP500Strategy
    {
        public override CompletionDetectionMode CompletionMode => CompletionDetectionMode.ExitFile;

        public override async Task<bool> ShouldAutoProcess(Scanner scanner, string directoryPath)
        {
            // Auto-processing is handled by SP500ExporterService, not by FileSystemWatcherService.
            // This returns false because the watcher-based auto-process path is not used.
            return await Task.FromResult(false);
        }
    }
}
