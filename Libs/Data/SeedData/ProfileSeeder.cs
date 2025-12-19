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
                    ProfileName = "HS-1800 Auto",
                    StrategyClassName = "HS1800Strategy",
                    Description = "Automatic processing for HS-1800 scanners with daily folder structure"
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

            // Add default configurations for HS-1800
            var hs1800Profile = profiles.First(p => p.StrategyClassName == "HS1800Strategy");
            var hs1800Configs = new List<ProfileConfiguration>
            {
                new ProfileConfiguration
                {
                    ProfileId = hs1800Profile.Id,
                    ConfigKey = "CompletionDelaySeconds",
                    ConfigValue = "25",
                    Description = "Time to wait after directory creation before processing"
                },
                new ProfileConfiguration
                {
                    ProfileId = hs1800Profile.Id,
                    ConfigKey = "DirectoryPattern",
                    ConfigValue = "{WatchedDir}/{YYYYMMDD}/*",
                    Description = "Expected directory structure pattern"
                }
            };

            context.ProfileConfigurations.AddRange(hs1800Configs);
            await context.SaveChangesAsync();
        }
    }
}
