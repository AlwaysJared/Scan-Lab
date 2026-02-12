using Libs.Classes;
using Libs.Data.Context;
using Libs.Data.Models;
using Libs.Helpers;
using Libs.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Libs.Repositories
{
    public class ScannerRepository : IScannerRepository, IDisposable
    {
        private ScanLabContext context;
        public ScannerRepository(ScanLabContext context)
        {
            this.context = context;
        }

        public async Task<SystemResponse> AddScanner(Scanner scnr)
        {
            try{
                // Validate profile if provided
                if (scnr.ProfileId.HasValue)
                {
                    var profile = await context.ScannerProfiles
                        .FirstOrDefaultAsync(p => p.Id == scnr.ProfileId.Value);

                    if (profile == null)
                        return new SystemResponse { IsSuccess = false, Message = "Selected profile not found" };
                }

                context.Scanners.Add(scnr);
                await context.SaveChangesAsync();
            }
            catch(Exception ex){
                return new SystemResponse{IsSuccess = false, Message = ex.Message};
            }

            return new SystemResponse{IsSuccess = true};
        }

        public async Task<SystemResponse> DeleteScanner(Guid id)
        {
            try{
                var scnr = await context.Scanners.Where(s => s.Id == id).FirstOrDefaultAsync();

                if(scnr == null){
                    return new SystemResponse{
                        IsSuccess = false,
                        Message = "Scanner not found"
                    };
                }

                context.Scanners.Remove(scnr);
                await context.SaveChangesAsync();

                return new SystemResponse{
                    IsSuccess = true,
                };
            }
            catch(Exception ex){
                return new SystemResponse{
                    IsSuccess =false,
                    Message = ex.Message
                };
            }
        }

        public void Dispose()
        {
            context.Dispose();
        }

        public Scanner GetScanner(Guid id)
        {
            throw new NotImplementedException();
        }

        public async Task<List<Scanner>> GetScanners()
        {
            return await context.Scanners.Include(s => s.Profile).ToListAsync();
        }

        public async Task<List<ScannerProfile>> GetProfiles()
        {
            return await context.ScannerProfiles.Where(p => p.IsActive).ToListAsync();
        }

        public async Task<List<ProfileConfiguration>> GetProfileConfigurations(Guid profileId)
        {
            return await context.ProfileConfigurations.Where(pc => pc.ProfileId == profileId).ToListAsync();
        }

        public async Task<SystemResponse> UpdateProfileConfiguration(Guid configId, string value)
        {
            try
            {
                var config = await context.ProfileConfigurations.FindAsync(configId);
                if (config == null)
                    return new SystemResponse { IsSuccess = false, Message = "Configuration not found" };

                config.ConfigValue = value;
                await context.SaveChangesAsync();
                return new SystemResponse { IsSuccess = true };
            }
            catch (Exception ex)
            {
                return new SystemResponse { IsSuccess = false, Message = ex.Message };
            }
        }

        public void Save()
        {
            throw new NotImplementedException();
        }

        public async Task<SystemResponse> UpdateScanner(Scanner scanner)
        {
            try
            {
                var dbScnr = context.Scanners.Where(s => s.Id == scanner.Id).FirstOrDefault();

                if(dbScnr == null){
                    return new SystemResponse{IsSuccess = false, Message = "Scanner not found"}; 
                }


                var compatibleWatchedDir = scanner.WatchedDir;
                var compatibleDestDir = scanner.DestinationDir;
                var compatibleArchiveDir = scanner.ArchiveDir;


                if (!Directory.Exists(compatibleWatchedDir))
                {
                    compatibleWatchedDir = IOHelpers.NetworkPathConverter.ResolvePath(scanner.WatchedDir);

                    if (compatibleWatchedDir == null)
                        return new SystemResponse { IsSuccess = false, Message = $"Could not find path {scanner.WatchedDir} on server" };
                }


                if (!Directory.Exists(compatibleDestDir))
                {
                    compatibleDestDir = IOHelpers.NetworkPathConverter.ResolvePath(scanner.DestinationDir);
                    
                    if (compatibleDestDir == null)
                        return new SystemResponse { IsSuccess = false, Message = $"Could not find path {scanner.DestinationDir} on server" };
                }


                if (!Directory.Exists(compatibleArchiveDir))
                {
                    compatibleArchiveDir = IOHelpers.NetworkPathConverter.ResolvePath(scanner.ArchiveDir);
                    
                    if (compatibleArchiveDir == null)
                        return new SystemResponse { IsSuccess = false, Message = $"Could not find path {scanner.ArchiveDir} on server" };
                }
                    

                dbScnr.ScannerName = scanner.ScannerName;
                dbScnr.Make = scanner.Make;
                dbScnr.Model = scanner.Model;
                dbScnr.WatchedDir = compatibleWatchedDir;
                dbScnr.DestinationDir = compatibleDestDir;
                dbScnr.ArchiveDir = compatibleArchiveDir;
                dbScnr.ArtistName = scanner.ArtistName;
                dbScnr.ProfileId = scanner.ProfileId;
                dbScnr.AutoProcessDelaySeconds = scanner.AutoProcessDelaySeconds;

                await context.SaveChangesAsync();

                return new SystemResponse { IsSuccess = true};
            }
            catch (Exception ex)
            {
                
                return new SystemResponse{IsSuccess = false, Message = ex.Message};
            }
        }
    }
}