using Libs.Classes;
using Libs.Data.Context;
using Libs.Data.Models;
using Libs.Interfaces;
using Libs.Services.ScannerStrategies;
using Microsoft.EntityFrameworkCore;

namespace Libs.Repositories
{
    /// <summary>
    /// Repository for managing scanner profiles and their configurations.
    /// </summary>
    public class ProfileRepository : IProfileRepository, IDisposable
    {
        private readonly ScanLabContext context;

        public ProfileRepository(ScanLabContext context)
        {
            this.context = context;
        }

        /// <summary>
        /// Gets all active scanner profiles ordered by name.
        /// </summary>
        public async Task<List<ScannerProfile>> GetProfiles()
        {
            return await context.ScannerProfiles
                .Where(p => p.IsActive)
                .OrderBy(p => p.ProfileName)
                .ToListAsync();
        }

        /// <summary>
        /// Gets a single scanner profile by ID.
        /// </summary>
        public async Task<ScannerProfile?> GetProfile(Guid id)
        {
            return await context.ScannerProfiles
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        /// <summary>
        /// Adds a new scanner profile. Validates strategy class name and checks for duplicate names.
        /// </summary>
        public async Task<SystemResponse> AddProfile(ScannerProfile profile)
        {
            try
            {
                if (!ScannerStrategyFactory.IsValidStrategy(profile.StrategyClassName))
                {
                    return new SystemResponse
                    {
                        IsSuccess = false,
                        Message = $"Invalid strategy class name: {profile.StrategyClassName}"
                    };
                }

                var duplicate = await context.ScannerProfiles
                    .FirstOrDefaultAsync(p => p.ProfileName == profile.ProfileName && p.IsActive);

                if (duplicate != null)
                {
                    return new SystemResponse
                    {
                        IsSuccess = false,
                        Message = $"Profile with name '{profile.ProfileName}' already exists"
                    };
                }

                profile.DateCreated = DateTime.UtcNow;

                context.ScannerProfiles.Add(profile);
                await context.SaveChangesAsync();

                return new SystemResponse
                {
                    IsSuccess = true,
                    ReturnObject = profile
                };
            }
            catch (Exception ex)
            {
                return new SystemResponse
                {
                    IsSuccess = false,
                    Message = ex.Message
                };
            }
        }

        /// <summary>
        /// Updates an existing scanner profile's name, strategy, and description.
        /// </summary>
        public async Task<SystemResponse> UpdateProfile(ScannerProfile profile)
        {
            try
            {
                var dbProfile = await context.ScannerProfiles
                    .FirstOrDefaultAsync(p => p.Id == profile.Id);

                if (dbProfile == null)
                {
                    return new SystemResponse
                    {
                        IsSuccess = false,
                        Message = "Profile not found"
                    };
                }

                if (!ScannerStrategyFactory.IsValidStrategy(profile.StrategyClassName))
                {
                    return new SystemResponse
                    {
                        IsSuccess = false,
                        Message = $"Invalid strategy class name: {profile.StrategyClassName}"
                    };
                }

                dbProfile.ProfileName = profile.ProfileName;
                dbProfile.StrategyClassName = profile.StrategyClassName;
                dbProfile.Description = profile.Description;
                dbProfile.DateUpdated = DateTime.UtcNow;

                await context.SaveChangesAsync();

                return new SystemResponse { IsSuccess = true };
            }
            catch (Exception ex)
            {
                return new SystemResponse
                {
                    IsSuccess = false,
                    Message = ex.Message
                };
            }
        }

        /// <summary>
        /// Soft-deletes a scanner profile. Blocked if any scanners are currently using it.
        /// </summary>
        public async Task<SystemResponse> DeleteProfile(Guid id)
        {
            try
            {
                var profile = await context.ScannerProfiles
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (profile == null)
                {
                    return new SystemResponse
                    {
                        IsSuccess = false,
                        Message = "Profile not found"
                    };
                }

                var scannersUsingProfile = await context.Scanners
                    .Where(s => s.ProfileId == id)
                    .CountAsync();

                if (scannersUsingProfile > 0)
                {
                    return new SystemResponse
                    {
                        IsSuccess = false,
                        Message = $"Cannot delete profile: {scannersUsingProfile} scanner(s) are using this profile"
                    };
                }

                // Soft delete
                profile.IsActive = false;
                profile.DateUpdated = DateTime.UtcNow;

                await context.SaveChangesAsync();

                return new SystemResponse { IsSuccess = true };
            }
            catch (Exception ex)
            {
                return new SystemResponse
                {
                    IsSuccess = false,
                    Message = ex.Message
                };
            }
        }

        /// <summary>
        /// Gets all configuration key-value pairs for a profile, ordered by key.
        /// </summary>
        public async Task<List<ProfileConfiguration>> GetProfileConfigurations(Guid profileId)
        {
            return await context.ProfileConfigurations
                .Where(pc => pc.ProfileId == profileId)
                .OrderBy(pc => pc.ConfigKey)
                .ToListAsync();
        }

        /// <summary>
        /// Updates a profile configuration's value and description.
        /// </summary>
        public async Task<SystemResponse> UpdateProfileConfiguration(ProfileConfiguration config)
        {
            try
            {
                var dbConfig = await context.ProfileConfigurations
                    .FirstOrDefaultAsync(pc => pc.Id == config.Id);

                if (dbConfig == null)
                {
                    return new SystemResponse
                    {
                        IsSuccess = false,
                        Message = "Configuration not found"
                    };
                }

                dbConfig.ConfigValue = config.ConfigValue;
                dbConfig.Description = config.Description;

                await context.SaveChangesAsync();

                return new SystemResponse { IsSuccess = true };
            }
            catch (Exception ex)
            {
                return new SystemResponse
                {
                    IsSuccess = false,
                    Message = ex.Message
                };
            }
        }

        public void Dispose()
        {
            context.Dispose();
        }
    }
}
