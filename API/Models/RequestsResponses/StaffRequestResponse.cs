using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Libs.Data.Models;

namespace API.Models.RequestsResponses
{
    // public class StaffRequestResponse
    // {

    // }

    public class RegisterStaffRequest : BaseRequest
    {
        public required string UserName { get; set; }
        public string? Email { get; set; }
        public required string Password { get; set; }
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
    }

    public class GetStaffRequest : BaseRequest
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class GetStaffResponse : BaseResponse
    {
        public int TotalPages { get; set; } = 0;
        public List<Staff> Staff { get; set; } = new List<Staff>();
    }
}