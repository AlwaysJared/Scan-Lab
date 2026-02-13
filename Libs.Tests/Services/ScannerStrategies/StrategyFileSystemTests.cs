using Libs.Services.ScannerStrategies;
using Libs.Tests.Helpers;
using NUnit.Framework;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Libs.Tests.Services.ScannerStrategies
{
    [TestFixture]
    [Category("FileSystem")]
    public class StrategyFileSystemTests : FileSystemTestBase
    {
        #region NoritsuControllerStrategy

        [Test]
        public void Noritsu_ResolveWatchPath_CreatesDailyFolder()
        {
            var strategy = new NoritsuControllerStrategy();
            var scanner = TestDataBuilder.CreateTestScanner(watchedDir: TestDirectory);

            var watchPath = strategy.ResolveWatchPath(scanner);

            var expectedFolder = DateTime.Now.ToString("yyyyMMdd");
            Assert.That(watchPath, Is.EqualTo(Path.Combine(TestDirectory, expectedFolder)));
            Assert.That(Directory.Exists(watchPath), Is.True);
        }

        [Test]
        public void Noritsu_ResolveWatchPath_FolderAlreadyExists()
        {
            var strategy = new NoritsuControllerStrategy();
            var scanner = TestDataBuilder.CreateTestScanner(watchedDir: TestDirectory);

            // Create the daily folder first
            var dailyFolder = Path.Combine(TestDirectory, DateTime.Now.ToString("yyyyMMdd"));
            Directory.CreateDirectory(dailyFolder);

            // Should not throw when folder already exists
            var watchPath = strategy.ResolveWatchPath(scanner);

            Assert.That(watchPath, Is.EqualTo(dailyFolder));
            Assert.That(Directory.Exists(watchPath), Is.True);
        }

        [Test]
        public async Task Noritsu_GetLatestRollDirectory_ReturnsNewest()
        {
            var strategy = new NoritsuControllerStrategy();
            var scanner = TestDataBuilder.CreateTestScanner(watchedDir: TestDirectory);

            // Create daily folder
            var dailyFolder = FileSystemTestHelper.CreateHS1800Structure(TestDirectory);

            // Create two roll folders with different timestamps
            var olderRoll = Path.Combine(dailyFolder, "OlderRoll");
            Directory.CreateDirectory(olderRoll);
            Directory.SetLastWriteTimeUtc(olderRoll, DateTime.UtcNow.AddMinutes(-10));

            // Small delay to ensure different timestamps
            Thread.Sleep(50);

            var newerRoll = Path.Combine(dailyFolder, "NewerRoll");
            Directory.CreateDirectory(newerRoll);

            var latestDir = await strategy.GetLatestRollDirectory(scanner);

            Assert.That(latestDir, Is.EqualTo(newerRoll));
        }

        [Test]
        public async Task Noritsu_GetLatestRollDirectory_EmptyDir_ReturnsNull()
        {
            var strategy = new NoritsuControllerStrategy();
            var scanner = TestDataBuilder.CreateTestScanner(watchedDir: TestDirectory);

            // Create daily folder but no subdirectories
            FileSystemTestHelper.CreateHS1800Structure(TestDirectory);

            var latestDir = await strategy.GetLatestRollDirectory(scanner);

            Assert.That(latestDir, Is.Null);
        }

        #endregion

        #region SP500Strategy

        [Test]
        public void SP500_ResolveWatchPath_ReturnsWatchedDir()
        {
            var strategy = new SP500Strategy();
            var scanner = TestDataBuilder.CreateTestScanner(watchedDir: TestDirectory);

            var watchPath = strategy.ResolveWatchPath(scanner);

            Assert.That(watchPath, Is.EqualTo(TestDirectory));
        }

        [Test]
        public async Task SP500_GetLatestRollDirectory_ReturnsNewest()
        {
            var strategy = new SP500Strategy();
            var scanner = TestDataBuilder.CreateTestScanner(watchedDir: TestDirectory);

            // Create two roll directories
            var olderRoll = Path.Combine(TestDirectory, "Roll_001");
            Directory.CreateDirectory(olderRoll);
            Directory.SetLastWriteTimeUtc(olderRoll, DateTime.UtcNow.AddMinutes(-10));

            Thread.Sleep(50);

            var newerRoll = Path.Combine(TestDirectory, "Roll_002");
            Directory.CreateDirectory(newerRoll);

            var latestDir = await strategy.GetLatestRollDirectory(scanner);

            Assert.That(latestDir, Is.EqualTo(newerRoll));
        }

        [Test]
        public async Task SP500_GetLatestRollDirectory_NoDir_ReturnsNull()
        {
            var strategy = new SP500Strategy();
            var nonExistentDir = Path.Combine(TestDirectory, "does_not_exist");
            var scanner = TestDataBuilder.CreateTestScanner(watchedDir: nonExistentDir);

            var latestDir = await strategy.GetLatestRollDirectory(scanner);

            Assert.That(latestDir, Is.Null);
        }

        #endregion
    }
}
