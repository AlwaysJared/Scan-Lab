using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Libs.Classes;
using Libs.Data.Context;
using Libs.Data.Models;
using Libs.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Libs.Repositories
{
    public class StaffRepository : IStaffRepository
    {
        private ScanLabContext context;
        public StaffRepository(ScanLabContext context)
        {
            this.context = context;
        }

        // public Task<SystemResponse> AddStaff(Staff staff)
        // {
        //     throw new NotImplementedException();
        // }

        public void Dispose()
        {
            context.Dispose();
        }

        public Task<SystemResponse> EditStaff(Staff staff)
        {
            throw new NotImplementedException();
        }

        public async Task<SystemResponse> GetStaff(Guid? staffId, int? page = 1, int? pageSize = 10)
        {
            try
            {
                var staff = await context.Staff.Where(s =>
                    staffId.HasValue ? s.Id == staffId : true)
                    .ToListAsync();

                var totalPages = (int)Math.Ceiling((staff?.Count ?? 0) / (double)pageSize);

                // if (staff.Count > ((page.Value - 1) * pageSize.Value))
                // {
                //     staff = staff.Skip((page.Value - 1) * pageSize.Value).ToList();
                // }

                staff = staff.Skip((page.Value-1) * pageSize.Value).Take(pageSize.Value).ToList();

                return new SystemResponse
                {
                    IsSuccess = true,
                    ReturnObject = new
                    {
                        Staff = staff,
                        TotalPages = totalPages,
                    }
                };
            }
            catch (Exception ex)
            {
                return new SystemResponse
                {
                    IsSuccess = false,
                    IsError = true,
                    Message = ex.Message,
                    ReturnObject = ex
                };
            }
        }
    }
}