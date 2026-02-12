using Libs.Classes;
using Libs.Data.Models;

namespace Libs.Interfaces
{
    public interface IProfileRepository : IDisposable
    {
        Task<List<ScannerProfile>> GetProfiles();
        Task<ScannerProfile?> GetProfile(Guid id);
        Task<SystemResponse> AddProfile(ScannerProfile profile);
        Task<SystemResponse> UpdateProfile(ScannerProfile profile);
        Task<SystemResponse> DeleteProfile(Guid id);
        Task<List<ProfileConfiguration>> GetProfileConfigurations(Guid profileId);
        Task<SystemResponse> UpdateProfileConfiguration(ProfileConfiguration config);
    }
}
