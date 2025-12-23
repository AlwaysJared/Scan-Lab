using Libs.Data.Context;
using Libs.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Libs.Data.SeedData
{
    public static class ProfileSeeder
    {
        public static async Task SeedProfiles(ScanLabContext context)
        {
            if (await context.ScannerProfiles.AnyAsync())
                return; // Already seeded

            var profiles = new List<ScannerProfile>
            {
                new ScannerProfile
                {
                    ProfileName = "Noritsu Controller Auto",
                    StrategyClassName = "NoritsuControllerStrategy",
                    Description = "Automatic processing for Noritsu Controller-based scanners (HS-1800, LS-600, etc.) with daily folder structure"
                },
                new ScannerProfile
                {
                    ProfileName = "SP-500 Manual",
                    StrategyClassName = "SP500Strategy",
                    Description = "Manual processing for SP-500 scanners"
                },
                new ScannerProfile
                {
                    ProfileName = "SP-3000 Manual",
                    StrategyClassName = "SP3000Strategy",
                    Description = "Manual processing for SP-3000 scanners"
                }
            };

            context.ScannerProfiles.AddRange(profiles);
            await context.SaveChangesAsync();

            // Add default configurations for Noritsu Controller scanners
            var noritsuProfile = profiles.First(p => p.StrategyClassName == "NoritsuControllerStrategy");
            var noritsuConfigs = new List<ProfileConfiguration>
            {
                new ProfileConfiguration
                {
                    ProfileId = noritsuProfile.Id,
                    ConfigKey = "CompletionDelaySeconds",
                    ConfigValue = "25",
                    Description = "Time to wait after last file creation before processing"
                },
                new ProfileConfiguration
                {
                    ProfileId = noritsuProfile.Id,
                    ConfigKey = "DirectoryPattern",
                    ConfigValue = "{WatchedDir}/{YYYYMMDD}/*",
                    Description = "Expected directory structure pattern"
                }
            };

            context.ProfileConfigurations.AddRange(noritsuConfigs);
            await context.SaveChangesAsync();
        }
    }
}
