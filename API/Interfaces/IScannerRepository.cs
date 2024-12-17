using API.Models;
using Libs.Classes;
using Libs.Data.Models;

interface IScannerRepository : IDisposable
{
    Task<List<Scanner>> GetScanners();
    Scanner GetScanner(Guid id);
    Task<SystemResponse> AddScanner(AddScannerRequest scanner);
    Task<SystemResponse> DeleteScanner(Guid id);
    Task<SystemResponse> UpdateScanner(Scanner scanner);
    void Save();
}