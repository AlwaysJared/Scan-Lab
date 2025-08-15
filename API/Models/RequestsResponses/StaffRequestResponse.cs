using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace API.Models.RequestsResponses
{
    // public class StaffRequestResponse
    // {

    // }

    public class RegisterStaffRequest
    {
        public required string UserName { get; set; }
        public string? Email { get; set; }
        public required string Password { get; set; }
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
    }
}