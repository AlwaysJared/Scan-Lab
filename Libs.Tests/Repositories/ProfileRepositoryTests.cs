using Libs.Data.Models;
using Libs.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Libs.Tests.Repositories
{
    [TestFixture]
    [Category("Integration")]
    public class ProfileRepositoryTests : DatabaseTestBase
    {
        [Test]
        public async Task SaveAndRetrieve_ScannerProfile()
        {
            var profile = TestDataBuilder.CreateTestProfile(
                profileName: "Test Noritsu",
                strategyClassName: "NoritsuControllerStrategy",
                description: "Integration test profile");

            DbFixture.Context.ScannerProfiles.Add(profile);
            await DbFixture.Context.SaveChangesAsync();

            var retrieved = await DbFixture.Context.ScannerProfiles
                .FirstOrDefaultAsync(p => p.Id == profile.Id);

            Assert.That(retrieved, Is.Not.Null);
            Assert.That(retrieved!.ProfileName, Is.EqualTo("Test Noritsu"));
            Assert.That(retrieved.StrategyClassName, Is.EqualTo("NoritsuControllerStrategy"));
            Assert.That(retrieved.Description, Is.EqualTo("Integration test profile"));
            Assert.That(retrieved.IsActive, Is.True);
        }

        [Test]
        public async Task SaveAndRetrieve_ProfileConfiguration()
        {
            var profile = TestDataBuilder.CreateTestProfile();
            DbFixture.Context.ScannerProfiles.Add(profile);
            await DbFixture.Context.SaveChangesAsync();

            var config = TestDataBuilder.CreateTestProfileConfig(
                profile,
                configKey: "CompletionDelaySeconds",
                configValue: "30");
            DbFixture.Context.ProfileConfigurations.Add(config);
            await DbFixture.Context.SaveChangesAsync();

            var retrieved = await DbFixture.Context.ProfileConfigurations
                .Include(c => c.Profile)
                .FirstOrDefaultAsync(c => c.Id == config.Id);

            Assert.That(retrieved, Is.Not.Null);
            Assert.That(retrieved!.ConfigKey, Is.EqualTo("CompletionDelaySeconds"));
            Assert.That(retrieved.ConfigValue, Is.EqualTo("30"));
            Assert.That(retrieved.Profile, Is.Not.Null);
            Assert.That(retrieved.Profile!.Id, Is.EqualTo(profile.Id));
        }

        [Test]
        public async Task Scanner_WithProfile_Relationship()
        {
            var profile = TestDataBuilder.CreateTestProfile();
            DbFixture.Context.ScannerProfiles.Add(profile);
            await DbFixture.Context.SaveChangesAsync();

            var scanner = TestDataBuilder.CreateTestScanner(profile: profile);
            DbFixture.Context.Scanners.Add(scanner);
            await DbFixture.Context.SaveChangesAsync();

            var retrieved = await DbFixture.Context.Scanners
                .Include(s => s.Profile)
                .FirstOrDefaultAsync(s => s.Id == scanner.Id);

            Assert.That(retrieved, Is.Not.Null);
            Assert.That(retrieved!.Profile, Is.Not.Null);
            Assert.That(retrieved.Profile!.ProfileName, Is.EqualTo("Test Profile"));
            Assert.That(retrieved.ProfileId, Is.EqualTo(profile.Id));
        }

        [Test]
        public async Task DeleteProfile_CascadesConfigurations()
        {
            var profile = TestDataBuilder.CreateTestProfile();
            DbFixture.Context.ScannerProfiles.Add(profile);
            await DbFixture.Context.SaveChangesAsync();

            var config1 = TestDataBuilder.CreateTestProfileConfig(profile, "Key1", "Value1");
            var config2 = TestDataBuilder.CreateTestProfileConfig(profile, "Key2", "Value2");
            DbFixture.Context.ProfileConfigurations.AddRange(config1, config2);
            await DbFixture.Context.SaveChangesAsync();

            // Verify configs exist
            var configCount = await DbFixture.Context.ProfileConfigurations
                .Where(c => c.ProfileId == profile.Id)
                .CountAsync();
            Assert.That(configCount, Is.EqualTo(2));

            // Delete profile
            DbFixture.Context.ScannerProfiles.Remove(profile);
            await DbFixture.Context.SaveChangesAsync();

            // Verify configs are cascade-deleted
            var remainingConfigs = await DbFixture.Context.ProfileConfigurations
                .Where(c => c.ProfileId == profile.Id)
                .CountAsync();
            Assert.That(remainingConfigs, Is.EqualTo(0));
        }

        [Test]
        public async Task Profile_RequiredFields_Validation()
        {
            // Profile with all required fields should save
            var validProfile = TestDataBuilder.CreateTestProfile();
            DbFixture.Context.ScannerProfiles.Add(validProfile);
            await DbFixture.Context.SaveChangesAsync();

            var retrieved = await DbFixture.Context.ScannerProfiles.FindAsync(validProfile.Id);
            Assert.That(retrieved, Is.Not.Null);
            Assert.That(retrieved!.ProfileName, Is.Not.Null.And.Not.Empty);
            Assert.That(retrieved.StrategyClassName, Is.Not.Null.And.Not.Empty);
        }
    }
}
