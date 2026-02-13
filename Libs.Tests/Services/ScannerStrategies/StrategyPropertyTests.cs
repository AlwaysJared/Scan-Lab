using Libs.Data.Models;
using Libs.Enums;
using Libs.Services.ScannerStrategies;
using Libs.Tests.Helpers;
using NUnit.Framework;
using System.Threading.Tasks;

namespace Libs.Tests.Services.ScannerStrategies
{
    [TestFixture]
    [Category("Unit")]
    public class StrategyPropertyTests
    {
        #region NoritsuControllerStrategy

        [Test]
        public void NoritsuController_CompletionMode_IsTimeBasedDelay()
        {
            var strategy = new NoritsuControllerStrategy();

            Assert.That(strategy.CompletionMode, Is.EqualTo(CompletionDetectionMode.TimeBasedDelay));
        }

        [Test]
        public void NoritsuController_IsRecursive_True()
        {
            var strategy = new NoritsuControllerStrategy();

            Assert.That(strategy.IsRecursive, Is.True);
        }

        [Test]
        public void NoritsuController_CompletionDelaySeconds_IsNull()
        {
            var strategy = new NoritsuControllerStrategy();

            Assert.That(strategy.CompletionDelaySeconds, Is.Null);
        }

        #endregion

        #region SP500Strategy

        [Test]
        public void SP500_CompletionMode_IsManual()
        {
            var strategy = new SP500Strategy();

            Assert.That(strategy.CompletionMode, Is.EqualTo(CompletionDetectionMode.Manual));
        }

        [Test]
        public void SP500_IsRecursive_False()
        {
            var strategy = new SP500Strategy();

            Assert.That(strategy.IsRecursive, Is.False);
        }

        [Test]
        public async Task SP500_ShouldAutoProcess_ReturnsFalse()
        {
            var strategy = new SP500Strategy();
            var scanner = TestDataBuilder.CreateTestScanner();

            var result = await strategy.ShouldAutoProcess(scanner, "/some/path");

            Assert.That(result, Is.False);
        }

        #endregion

        #region SP500AutoStrategy

        [Test]
        public void SP500Auto_CompletionMode_IsExitFile()
        {
            var strategy = new SP500AutoStrategy();

            Assert.That(strategy.CompletionMode, Is.EqualTo(CompletionDetectionMode.ExitFile));
        }

        [Test]
        public async Task SP500Auto_ShouldAutoProcess_ReturnsFalse()
        {
            var strategy = new SP500AutoStrategy();
            var scanner = TestDataBuilder.CreateTestScanner();

            var result = await strategy.ShouldAutoProcess(scanner, "/some/path");

            Assert.That(result, Is.False);
        }

        #endregion

        #region SP3000Strategy

        [Test]
        public void SP3000_InheritsFromSP500()
        {
            var strategy = new SP3000Strategy();

            Assert.That(strategy, Is.InstanceOf<SP500Strategy>());
        }

        [Test]
        public void SP3000_CompletionMode_IsManual()
        {
            var strategy = new SP3000Strategy();

            Assert.That(strategy.CompletionMode, Is.EqualTo(CompletionDetectionMode.Manual));
        }

        #endregion
    }
}
