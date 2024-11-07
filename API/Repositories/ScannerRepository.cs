using Libs.Classes;
using Libs.Data.Context;
using Libs.Data.Models;
using Microsoft.EntityFrameworkCore;

public class ScannerRepository : IScannerRepository, IDisposable
{
    private ScanLabContext context;
    public ScannerRepository(ScanLabContext context)
    {
        this.context = context;
    }

    public Task<SystemResponse> AddScanner(Scanner scanner)
    {
        throw new NotImplementedException();
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
        throw new NotImplementedException();
    }

    public Scanner GetScanner(Guid id)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<Scanner>> GetScanners()
    {
        throw new NotImplementedException();
    }

    public void Save()
    {
        throw new NotImplementedException();
    }

    public Task<SystemResponse> UpdateScanner(Scanner scanner)
    {
        throw new NotImplementedException();
    }
}