using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Libs.Services.SP500Export
{
    /// <summary>
    /// Handles cleanup of Frontier source directories after image export.
    /// </summary>
    public static class SP500Cleanup
    {
        /// <summary>
        /// Deletes the specified directories (typically -1-4, -Ac_ImgConv, -1-1).
        /// </summary>
        public static Task DeleteDirectoriesAsync(List<string> directories, ILogger? logger = null)
        {
            if (directories == null || directories.Count == 0)
            {
                logger?.LogDebug("No directories to clean up");
                return Task.CompletedTask;
            }

            foreach (var dir in directories)
            {
                try
                {
                    if (Directory.Exists(dir))
                    {
                        Directory.Delete(dir, true);
                        logger?.LogInformation("Deleted directory: {Dir}", Path.GetFileName(dir));
                    }
                }
                catch (Exception ex)
                {
                    logger?.LogError(ex, "Error deleting directory {Dir}", dir);
                }
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Cleans up orphaned -1-1 directories in the watch directory.
        /// Called on startup to improve polling performance.
        /// </summary>
        public static Task CleanupStaleDirectoriesAsync(string watchDirectory, ILogger? logger = null)
        {
            if (string.IsNullOrWhiteSpace(watchDirectory) || !Directory.Exists(watchDirectory))
            {
                logger?.LogWarning("Watch directory does not exist: {Path}", watchDirectory);
                return Task.CompletedTask;
            }

            try
            {
                var staleDirectories = Directory.GetDirectories(watchDirectory)
                    .Where(d => d.EndsWith("-1-1"))
                    .ToList();

                if (staleDirectories.Count == 0)
                {
                    logger?.LogDebug("No stale -1-1 directories to clean up");
                    return Task.CompletedTask;
                }

                logger?.LogInformation("Found {Count} stale -1-1 directories to clean up", staleDirectories.Count);

                int successCount = 0;
                int failCount = 0;

                foreach (var dir in staleDirectories)
                {
                    try
                    {
                        if (Directory.Exists(dir))
                        {
                            Directory.Delete(dir, true);
                            successCount++;
                            logger?.LogDebug("Deleted stale directory: {Dir}", Path.GetFileName(dir));
                        }
                    }
                    catch (Exception ex)
                    {
                        failCount++;
                        logger?.LogError(ex, "Error deleting stale directory {Dir}", Path.GetFileName(dir));
                    }
                }

                logger?.LogInformation("Stale directory cleanup complete: {Success} deleted, {Failed} failed",
                    successCount, failCount);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error during stale directory cleanup");
            }

            return Task.CompletedTask;
        }
    }
}
