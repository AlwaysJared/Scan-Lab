using Libs.Data.Models;
using Libs.Services.ScannerStrategies;
using Libs.Tests.Helpers;
using NUnit.Framework;
using System.Linq;

namespace Libs.Tests.Services.ScannerStrategies
{
    [TestFixture]
    [Category("Unit")]
    public class StrategyFactoryTests
    {
        [TestCase("NoritsuControllerStrategy")]
        [TestCase("SP500Strategy")]
        [TestCase("SP3000Strategy")]
        [TestCase("SP500AutoStrategy")]
        public void IsValidStrategy_WithRegisteredName_ReturnsTrue(string strategyName)
        {
            Assert.That(ScannerStrategyFactory.IsValidStrategy(strategyName), Is.True);
        }

        [TestCase("InvalidStrategy")]
        [TestCase("")]
        [TestCase("noritsucontrollerstrategy")] // case-sensitive
        public void IsValidStrategy_WithInvalidName_ReturnsFalse(string strategyName)
        {
            Assert.That(ScannerStrategyFactory.IsValidStrategy(strategyName), Is.False);
        }

        [Test]
        public void GetAvailableStrategies_ReturnsAllRegistered()
        {
            var strategies = ScannerStrategyFactory.GetAvailableStrategies();

            Assert.That(strategies, Has.Count.EqualTo(4));
            Assert.That(strategies, Does.Contain("NoritsuControllerStrategy"));
            Assert.That(strategies, Does.Contain("SP500Strategy"));
            Assert.That(strategies, Does.Contain("SP3000Strategy"));
            Assert.That(strategies, Does.Contain("SP500AutoStrategy"));
        }

        [TestCase("NoritsuControllerStrategy", typeof(NoritsuControllerStrategy))]
        [TestCase("SP500Strategy", typeof(SP500Strategy))]
        [TestCase("SP3000Strategy", typeof(SP3000Strategy))]
        [TestCase("SP500AutoStrategy", typeof(SP500AutoStrategy))]
        public void CreateStrategy_ByName_ReturnsCorrectType(string strategyName, System.Type expectedType)
        {
            var strategy = ScannerStrategyFactory.CreateStrategy(strategyName);

            Assert.That(strategy, Is.Not.Null);
            Assert.That(strategy, Is.TypeOf(expectedType));
        }

        [Test]
        public void CreateStrategy_InvalidName_ReturnsNull()
        {
            var strategy = ScannerStrategyFactory.CreateStrategy("NonExistentStrategy");

            Assert.That(strategy, Is.Null);
        }

        [Test]
        public void CreateStrategy_FromScanner_WithProfile_ReturnsStrategy()
        {
            var profile = TestDataBuilder.CreateTestProfile(
                strategyClassName: "NoritsuControllerStrategy");
            var scanner = TestDataBuilder.CreateTestScanner(profile: profile);

            var strategy = ScannerStrategyFactory.CreateStrategy(scanner);

            Assert.That(strategy, Is.Not.Null);
            Assert.That(strategy, Is.TypeOf<NoritsuControllerStrategy>());
        }

        [Test]
        public void CreateStrategy_FromScanner_WithoutProfile_ReturnsNull()
        {
            var scanner = TestDataBuilder.CreateTestScanner();

            var strategy = ScannerStrategyFactory.CreateStrategy(scanner);

            Assert.That(strategy, Is.Null);
        }
    }
}
