using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Libs.Services.SP500Export
{
    /// <summary>
    /// Scans the Frontier network share for roll directory sets.
    /// A valid set consists of three directories: {roll}-1-4, {roll}-Ac_ImgConv, {roll}-1-1.
    /// </summary>
    public class OuterPoller
    {
        private readonly string _watchDirectory;
        private readonly ILogger? _logger;

        public List<string> CreatedDirectories { get; private set; } = new();
        public string? RollDir { get; private set; }
        public string? RollNumber { get; private set; }
        public string? AcImgConv { get; private set; }
        public int Sensitivity { get; set; }

        public OuterPoller(string watchDirectory, int sensitivity, ILogger? logger = null)
        {
            _watchDirectory = watchDirectory;
            Sensitivity = sensitivity;
            _logger = logger;
        }

        /// <summary>
        /// Polls for a complete roll directory set. Returns true when found.
        /// </summary>
        public Task<bool> PollAsync()
        {
            return CheckForSet();
        }

        /// <summary>
        /// Checks the most recent directories for the three-directory pattern.
        /// </summary>
        public Task<bool> CheckForSet()
        {
            try
            {
                if (!Directory.Exists(_watchDirectory))
                {
                    _logger?.LogWarning("Watch directory does not exist: {Path}", _watchDirectory);
                    return Task.FromResult(false);
                }

                var recentDirectories = Directory.GetDirectories(_watchDirectory)
                    .Select(path => new { Path = path, Created = Directory.GetCreationTime(path) })
                    .OrderByDescending(d => d.Created)
                    .Take(Sensitivity)
                    .Select(d => d.Path)
                    .ToList();

                if (recentDirectories.Any(d => d.EndsWith("-1-4")) &&
                    recentDirectories.Any(d => d.EndsWith("-Ac_ImgConv")) &&
                    recentDirectories.Any(d => d.EndsWith("-1-1")))
                {
                    RollDir = recentDirectories.FirstOrDefault(d => d.EndsWith("-1-4"));
                    RollNumber = Path.GetFileName(RollDir)?[..^4]; // Strip "-1-4" suffix
                    AcImgConv = recentDirectories.FirstOrDefault(d => d.EndsWith("-Ac_ImgConv"));
                    CreatedDirectories = recentDirectories;

                    _logger?.LogInformation("Roll directory set detected: {RollNumber}", RollNumber);
                    return Task.FromResult(true);
                }

                return Task.FromResult(false);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error checking for roll directory set");
                return Task.FromResult(false);
            }
        }
    }
}
