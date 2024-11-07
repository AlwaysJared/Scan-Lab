using Libs.Classes;
using Libs.Data.Models;

interface IScannerRepository : IDisposable
{
    Task<IEnumerable<Scanner>> GetScanners();
    Scanner GetScanner(Guid id);
    Task<SystemResponse> AddScanner(Scanner scanner);
    Task<SystemResponse> DeleteScanner(Guid id);
    Task<SystemResponse> UpdateScanner(Scanner scanner);
    void Save();
}