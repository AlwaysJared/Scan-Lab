using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Libs.Classes;
using Libs.Data.Models;

namespace Libs.Interfaces
{
    public interface IStaffRepository : IDisposable
    {
        Task<SystemResponse> GetStaff(Guid? staffId, int? page, int? pageSize, string email, bool getAllStaff);
        // Task<SystemResponse> AddStaff(Staff staff);
        Task<SystemResponse> EditStaff(Staff staff);
    }
}