using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Libs.Services.SP500Export
{
    /// <summary>
    /// Monitors a specific roll's -1-4 subdirectories for the exit file (CdOrder.INF)
    /// and collects image paths from -Ac_ImgConv subdirectories.
    /// Detects if a new roll set appears during processing (interruption).
    /// </summary>
    public class InnerPoller
    {
        private readonly string _rollDir;
        private readonly string _acImgConv;
        private readonly string _outerPath;
        private readonly List<string> _createdDirectories;
        private readonly string _endOfExportFileName;
        private readonly int _sensitivity;
        private readonly ILogger? _logger;

        public string TargetDirectory { get; }
        public bool IsNewSet { get; private set; }
        public List<string> ImagesDetected { get; } = new();

        public InnerPoller(
            string rollDir,
            string acImgConv,
            string stagingDirectory,
            string endOfExportFileName,
            string outerPath,
            List<string> createdDirectories,
            int sensitivity = 3,
            ILogger? logger = null)
        {
            _rollDir = rollDir;
            _acImgConv = acImgConv;
            TargetDirectory = stagingDirectory;
            _endOfExportFileName = endOfExportFileName;
            _outerPath = outerPath;
            _createdDirectories = createdDirectories;
            _sensitivity = sensitivity;
            _logger = logger;
        }

        /// <summary>
        /// Single poll iteration. Returns true when polling should stop
        /// (exit file found or new set detected).
        /// </summary>
        public Task<bool> PollAsync()
        {
            bool isNewSetCheck = CheckForNewSet();
            bool isExitFileFound = ExitFileExists();

            if (isNewSetCheck && !isExitFileFound)
            {
                // New roll started before current one finished — interruption
                IsNewSet = true;
                _logger?.LogWarning("New roll set detected during export (interruption)");
                return Task.FromResult(true);
            }
            else if (isNewSetCheck && isExitFileFound)
            {
                // New roll started but exit file was found — save what we have
                IsNewSet = true;
                DetectAndSaveImagePaths();
                _logger?.LogInformation("Exit file detected with new roll set present");
                return Task.FromResult(true);
            }
            else if (!isNewSetCheck && isExitFileFound)
            {
                // Normal completion — exit file found, no interruption
                IsNewSet = false;
                DetectAndSaveImagePaths();
                _logger?.LogInformation("Exit file detected — roll export complete");
                return Task.FromResult(true);
            }

            // No exit file, no new set — keep polling
            IsNewSet = false;
            return Task.FromResult(false);
        }

        /// <summary>
        /// Checks subdirectories of -1-4 for the exit file.
        /// The Frontier software creates subdirectories inside -1-4 during color correction.
        /// </summary>
        private bool ExitFileExists()
        {
            try
            {
                if (!Directory.Exists(_rollDir))
                    return false;

                var innerDirs = Directory.GetDirectories(_rollDir);
                if (innerDirs.Length == 0)
                    return false;

                foreach (var dir in innerDirs)
                {
                    string exitFilePath = Path.Combine(dir, _endOfExportFileName);
                    if (File.Exists(exitFilePath))
                        return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error checking for exit file in {Dir}", _rollDir);
                return false;
            }
        }

        /// <summary>
        /// Checks if a new roll directory set has appeared (different from the current one).
        /// </summary>
        private bool CheckForNewSet()
        {
            try
            {
                if (!Directory.Exists(_outerPath))
                    return false;

                var recentDirectories = Directory.GetDirectories(_outerPath)
                    .Select(path => new { Path = path, Created = Directory.GetCreationTime(path) })
                    .OrderByDescending(d => d.Created)
                    .Take(_sensitivity)
                    .Select(d => d.Path)
                    .ToList();

                if (recentDirectories.Any(d => d.EndsWith("-1-4")) &&
                    recentDirectories.Any(d => d.EndsWith("-Ac_ImgConv")) &&
                    recentDirectories.Any(d => d.EndsWith("-1-1")))
                {
                    var recentSet = new HashSet<string>(recentDirectories, StringComparer.OrdinalIgnoreCase);
                    var createdSet = new HashSet<string>(_createdDirectories, StringComparer.OrdinalIgnoreCase);

                    // Same set = no new roll
                    return !recentSet.SetEquals(createdSet);
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error checking for new roll set");
                return false;
            }
        }

        /// <summary>
        /// Collects H*.jpg image paths from -Ac_ImgConv subdirectories.
        /// Each subdirectory (000001/, 000002/, etc.) contains one H*.jpg file.
        /// </summary>
        public void DetectAndSaveImagePaths()
        {
            try
            {
                if (!Directory.Exists(_acImgConv))
                {
                    _logger?.LogWarning("Ac_ImgConv directory not found: {Dir}", _acImgConv);
                    return;
                }

                var imageDirs = Directory.GetDirectories(_acImgConv);

                foreach (var dir in imageDirs)
                {
                    var imageFiles = ImageFileHelper.GetAllImageFiles(dir);
                    if (imageFiles.Count > 0)
                    {
                        string imageFile = imageFiles.First();
                        if (!ImagesDetected.Contains(imageFile))
                        {
                            _logger?.LogDebug("Image detected: {File}", Path.GetFileName(imageFile));
                            ImagesDetected.Add(imageFile);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error detecting images in {Dir}", _acImgConv);
            }
        }
    }
}
