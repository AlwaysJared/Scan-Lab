using Libs.Data.Models;
using Libs.Enums;
using System;

namespace Libs.Tests.Helpers
{
    /// <summary>
    /// Helper class to build test data objects with sensible defaults
    /// </summary>
    public static class TestDataBuilder
    {
        public static ScannerProfile CreateTestProfile(
            string profileName = "Test Profile",
            string strategyClassName = "NoritsuControllerStrategy",
            string description = "Test Description")
        {
            return new ScannerProfile
            {
                Id = Guid.NewGuid(),
                ProfileName = profileName,
                StrategyClassName = strategyClassName,
                Description = description,
                IsActive = true,
                DateCreated = DateTime.UtcNow
            };
        }

        public static ProfileConfiguration CreateTestProfileConfig(
            ScannerProfile profile,
            string configKey = "TestKey",
            string configValue = "TestValue")
        {
            return new ProfileConfiguration
            {
                Id = Guid.NewGuid(),
                ProfileId = profile.Id,
                Profile = profile,
                ConfigKey = configKey,
                ConfigValue = configValue,
                Description = "Test configuration"
            };
        }

        public static Scanner CreateTestScanner(
            string watchedDir = "/test/watched",
            string destinationDir = "/test/destination",
            string archiveDir = "/test/archive",
            ScannerProfile? profile = null,
            string scannerName = "Test Scanner",
            string make = "Test Make",
            string model = "Test Model")
        {
            return new Scanner
            {
                Id = Guid.NewGuid(),
                ScannerName = scannerName,
                Make = make,
                Model = model,
                WatchedDir = watchedDir,
                DestinationDir = destinationDir,
                ArchiveDir = archiveDir,
                ArtistName = "Test Artist",
                ProfileId = profile?.Id,
                Profile = profile
            };
        }

        public static Customer CreateTestCustomer(
            string firstName = "John",
            string lastName = "Doe")
        {
            return new Customer
            {
                Id = Guid.NewGuid(),
                FirstName = firstName,
                LastName = lastName
            };
        }

        public static Order CreateTestOrder(
            string? orderId = null,
            Scanner? scanner = null,
            Customer? customer = null,
            string customerInitials = "JD")
        {
            return new Order
            {
                OrderId = orderId ?? $"TEST{DateTime.Now.Ticks}",
                CustomerInitials = customerInitials,
                Customer = customer,
                Scanner = scanner,
                Status = OrderStatus.Created,
                DateCreated = DateTime.UtcNow
            };
        }

        public static Roll CreateTestRoll(
            Order? order = null,
            int rollNumber = 1,
            RollStatus status = RollStatus.Created)
        {
            return new Roll
            {
                RollId = Guid.NewGuid(),
                RollNumber = rollNumber,
                OrderId = order?.OrderId,
                Order = order,
                Status = status,
                DateCreated = DateTime.UtcNow
            };
        }
    }
}
