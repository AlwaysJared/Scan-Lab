using Libs.Data.Context;
using Libs.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Libs.Data.SeedData
{
    public static class ProfileSeeder
    {
        public static async Task SeedProfiles(ScanLabContext context)
        {
            // Seed each profile individually (idempotent by StrategyClassName)
            await SeedProfileIfMissing(context, new ScannerProfile
            {
                ProfileName = "Noritsu Controller Auto",
                StrategyClassName = "NoritsuControllerStrategy",
                Description = "Automatic processing for Noritsu Controller-based scanners (HS-1800, LS-600, etc.) with daily folder structure"
            }, new List<ProfileConfiguration>
            {
                new ProfileConfiguration
                {
                    ConfigKey = "CompletionDelaySeconds",
                    ConfigValue = "25",
                    Description = "Time to wait after last file creation before processing"
                },
                new ProfileConfiguration
                {
                    ConfigKey = "DirectoryPattern",
                    ConfigValue = "{WatchedDir}/{YYYYMMDD}/*",
                    Description = "Expected directory structure pattern"
                }
            });

            await SeedProfileIfMissing(context, new ScannerProfile
            {
                ProfileName = "SP-500 Manual",
                StrategyClassName = "SP500Strategy",
                Description = "Manual processing for SP-500 scanners"
            });

            await SeedProfileIfMissing(context, new ScannerProfile
            {
                ProfileName = "SP-3000 Manual",
                StrategyClassName = "SP3000Strategy",
                Description = "Manual processing for SP-3000 scanners"
            });

            await SeedProfileIfMissing(context, new ScannerProfile
            {
                ProfileName = "SP-500 Auto",
                StrategyClassName = "SP500AutoStrategy",
                Description = "Automatic export and processing for Fujifilm Frontier SP-500 scanners"
            }, new List<ProfileConfiguration>
            {
                new ProfileConfiguration
                {
                    ConfigKey = "FrontierSharePath",
                    ConfigValue = "",
                    Description = "Network share path where Frontier Software exports roll data"
                },
                new ProfileConfiguration
                {
                    ConfigKey = "EndOfExportFileName",
                    ConfigValue = "CdOrder.INF",
                    Description = "Exit file name that signals export completion"
                },
                new ProfileConfiguration
                {
                    ConfigKey = "TimeoutMinutes",
                    ConfigValue = "90",
                    Description = "Session timeout in minutes"
                },
                new ProfileConfiguration
                {
                    ConfigKey = "OuterPollerSensitivity",
                    ConfigValue = "3",
                    Description = "Number of recent directories to check for new rolls"
                },
                new ProfileConfiguration
                {
                    ConfigKey = "CopierMaxRetries",
                    ConfigValue = "72",
                    Description = "Max retry attempts for image copy operations"
                }
            });
        }

        private static async Task SeedProfileIfMissing(
            ScanLabContext context,
            ScannerProfile profile,
            List<ProfileConfiguration>? configurations = null)
        {
            var exists = await context.ScannerProfiles
                .AnyAsync(p => p.StrategyClassName == profile.StrategyClassName);

            if (exists)
                return;

            context.ScannerProfiles.Add(profile);
            await context.SaveChangesAsync();

            if (configurations != null && configurations.Count > 0)
            {
                foreach (var config in configurations)
                {
                    config.ProfileId = profile.Id;
                }

                context.ProfileConfigurations.AddRange(configurations);
                await context.SaveChangesAsync();
            }
        }
    }
}
