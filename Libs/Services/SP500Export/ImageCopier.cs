using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Libs.Services.SP500Export
{
    /// <summary>
    /// Copies detected images from Frontier Ac_ImgConv subdirectories to a flat staging directory.
    /// Includes retry logic for network file access reliability.
    /// </summary>
    public class ImageCopier
    {
        private readonly List<string> _imagePaths;
        private readonly string _targetDirectory;
        private readonly int _maxRetries;
        private readonly ILogger? _logger;

        public List<string> ImagesCopied { get; } = new();

        public ImageCopier(List<string> imagePaths, string targetDirectory, int maxRetries = 72, ILogger? logger = null)
        {
            _imagePaths = imagePaths;
            _targetDirectory = targetDirectory;
            _maxRetries = maxRetries;
            _logger = logger;
        }

        /// <summary>
        /// Copies all detected images to the target directory with retry logic.
        /// Returns true if all images were copied successfully.
        /// </summary>
        public async Task<bool> CopyImagesAsync(CancellationToken cancellationToken = default)
        {
            int retries = 0;

            while (!AllImagesCopied() && retries < _maxRetries)
            {
                if (cancellationToken.IsCancellationRequested)
                    return false;

                foreach (var imageFile in _imagePaths)
                {
                    if (ImagesCopied.Contains(imageFile))
                        continue;

                    try
                    {
                        var fileName = Path.GetFileName(imageFile);
                        var destinationPath = Path.Combine(_targetDirectory, fileName);
                        File.Copy(imageFile, destinationPath, true);
                        ImagesCopied.Add(imageFile);
                    }
                    catch (IOException ex)
                    {
                        _logger?.LogWarning("Error copying file {File}: {Message}", Path.GetFileName(imageFile), ex.Message);
                    }
                }

                if (!AllImagesCopied())
                {
                    retries++;
                    _logger?.LogDebug("Copy retry {Retry}/{Max}", retries, _maxRetries);
                    await Task.Delay(1000, cancellationToken);
                }
            }

            if (!AllImagesCopied())
            {
                _logger?.LogError("Failed to copy all images after {Retries} retries. Copied {Copied}/{Total}",
                    _maxRetries, ImagesCopied.Count, _imagePaths.Count);
                return false;
            }

            _logger?.LogInformation("All {Count} images copied successfully", ImagesCopied.Count);
            return true;
        }

        private bool AllImagesCopied()
        {
            var copiedImages = ImageFileHelper.GetAllImageFiles(_targetDirectory);
            var allImageNames = _imagePaths.Select(Path.GetFileName).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var copiedImageNames = copiedImages.Select(Path.GetFileName).ToHashSet(StringComparer.OrdinalIgnoreCase);
            return allImageNames.SetEquals(copiedImageNames);
        }
    }
}
