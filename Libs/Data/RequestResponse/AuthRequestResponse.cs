using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Libs.Data.RequestResponse.Base;

namespace Libs.Data.RequestResponse.Auth
{
    public class LoginRequest : BaseRequest
    {
        public required string Username { get; set; }
        public required string Password { get; set; }
    }

    public class LoginResponse : BaseResponse
    {
        public DateTime Expiration { get; set; }
    }
}