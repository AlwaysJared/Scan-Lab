using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Libs.Data.RequestResponse.Base
{
    public class BaseResponse
    {
        public bool Success { get; set; }
        public bool Warning { get; set; } = false;
        public string? Message { get; set; }
        public string? Token { get; set; }
    }

    public class BaseRequest
    {
        //Only used for authorized endpoints
        public string? Token { get; set; }
    }
}