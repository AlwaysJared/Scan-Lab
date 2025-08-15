using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API.Models.RequestsResponses
{
    public class LoginRequest : BaseRequest
    {
        public required string Username { get; set; }
        public required string Password { get; set; }
    }

    public class LoginResponse : BaseResponse
    {
        public DateTime Expiration {get; set;}
    }
}