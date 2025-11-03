using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Libs.Data.RequestResponse.Base;
using Libs.Data.Models;

namespace Libs.Data.RequestResponse.Staff
{
    public class GetStaffRequest : BaseRequest
    {
        public Guid? StaffId { get; set; }
        public int? Page { get; set; } = 1;
        public int? PageSize { get; set; } = 10;
        public bool GetAllStaff { get; set; } = false;
        public string? Query { get; set; }
    }

    public class GetStaffResponse : BaseResponse
    {
        public List<Models.Staff> Staff { get; set; } = new List<Models.Staff>();
        public int TotalPages { get; set; } = 0;
    }
}