using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Libs.Services.SP500Export
{
    /// <summary>
    /// Utility for discovering image files in a directory.
    /// </summary>
    public static class ImageFileHelper
    {
        private static readonly string[] ImageExtensions =
        {
            "*.jpg", "*.jpeg", "*.png", "*.bmp", "*.tif", "*.tiff"
        };

        public static List<string> GetAllImageFiles(string directory)
        {
            if (!Directory.Exists(directory))
                return new List<string>();

            return ImageExtensions
                .SelectMany(ext => Directory.GetFiles(directory, ext))
                .ToList();
        }
    }
}
