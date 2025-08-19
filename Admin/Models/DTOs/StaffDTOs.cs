using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Admin.Models.DTOs.Staff
{
    public class GetStaffDTO
    {
        public List<Libs.Data.Models.Staff> Staff { get; set; } = new List<Libs.Data.Models.Staff>();
        public int TotalPages { get; set; } = 0;
    }
}