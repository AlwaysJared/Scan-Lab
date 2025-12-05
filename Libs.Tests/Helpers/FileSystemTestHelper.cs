using System;
using System.IO;

namespace Libs.Tests.Helpers
{
    /// <summary>
    /// Helper class for file system operations in tests
    /// </summary>
    public static class FileSystemTestHelper
    {
        /// <summary>
        /// Creates a temporary test directory in the system temp folder
        /// </summary>
        public static string CreateTempTestDirectory()
        {
            var tempPath = Path.Combine(Path.GetTempPath(), $"ScanLabTest_{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempPath);
            return tempPath;
        }

        /// <summary>
        /// Creates a temporary directory with a specific name pattern
        /// </summary>
        public static string CreateTempTestDirectory(string folderName)
        {
            var tempPath = Path.Combine(Path.GetTempPath(), $"ScanLabTest_{folderName}_{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempPath);
            return tempPath;
        }

        /// <summary>
        /// Creates a test file with content
        /// </summary>
        public static string CreateTestFile(string directory, string fileName, string content = "test content")
        {
            var filePath = Path.Combine(directory, fileName);
            File.WriteAllText(filePath, content);
            return filePath;
        }

        /// <summary>
        /// Creates multiple test image files in a directory
        /// </summary>
        public static void CreateTestImages(string directory, int count, string extension = ".jpg")
        {
            for (int i = 1; i <= count; i++)
            {
                var fileName = $"IMG_{i:D4}{extension}";
                CreateTestFile(directory, fileName, $"Test image {i}");
            }
        }

        /// <summary>
        /// Safely deletes a directory and all contents
        /// </summary>
        public static void CleanupTestDirectory(string directory)
        {
            try
            {
                if (Directory.Exists(directory))
                {
                    Directory.Delete(directory, true);
                }
            }
            catch
            {
                // Ignore cleanup errors - test temp will be cleaned eventually
            }
        }

        /// <summary>
        /// Creates a daily folder structure like HS-1800 scanner
        /// </summary>
        public static string CreateHS1800Structure(string baseDir, DateTime? date = null)
        {
            var targetDate = date ?? DateTime.Now;
            var dailyFolder = Path.Combine(baseDir, targetDate.ToString("yyyyMMdd"));
            Directory.CreateDirectory(dailyFolder);
            return dailyFolder;
        }

        /// <summary>
        /// Creates a roll folder inside a directory with test images
        /// </summary>
        public static string CreateRollFolder(string parentDir, string rollName, int imageCount = 5)
        {
            var rollDir = Path.Combine(parentDir, rollName);
            Directory.CreateDirectory(rollDir);
            CreateTestImages(rollDir, imageCount);
            return rollDir;
        }
    }
}
