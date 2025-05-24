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
                // var scnr = new Scanner{
                //     Id = Guid.NewGuid(),
                //     ScannerName = req.ScannerName,
                //     Make = req.Make,
                //     Model = req.Model,
                //     WatchedDir = req.WatchedDir,
                //     DestinationDir = req.DestinationDir,
                //     ArchiveDir = req.ArchiveDir,
                //     ArtistName = req.ArtistName
                // };
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
            return await context.Scanners.ToListAsync();
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


                if(!Directory.Exists(compatibleWatchedDir))
                    compatibleWatchedDir = IOHelpers.NetworkPathConverter.ConvertToUncPath(scanner.WatchedDir);
                
                if(!Directory.Exists(compatibleDestDir))
                    compatibleDestDir = IOHelpers.NetworkPathConverter.ConvertToUncPath(scanner.DestinationDir);

                if(!Directory.Exists(compatibleArchiveDir))
                    compatibleArchiveDir = IOHelpers.NetworkPathConverter.ConvertToUncPath(scanner.ArchiveDir);

                dbScnr.ScannerName = scanner.ScannerName;
                dbScnr.Make = scanner.Make;
                dbScnr.Model = scanner.Model;
                dbScnr.WatchedDir = compatibleWatchedDir;
                dbScnr.DestinationDir = compatibleDestDir;
                dbScnr.ArchiveDir = compatibleArchiveDir;
                dbScnr.ArtistName = scanner.ArtistName;

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