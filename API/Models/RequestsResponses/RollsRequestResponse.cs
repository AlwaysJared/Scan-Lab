using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Libs.Enums;

namespace API.Models.RequestsResponses
{
    public class AddRollRequest : BaseRequest
    {
        public  required string OrderId { get; set; }
        public  required int RollNumber { get; set; }
    }

    public class CompleteRollRequest : BaseRequest
    {
        public Guid RollId { get; set; }
    }

    public class DeleteRollRequest : BaseRequest
    {
        public Guid RollId { get; set; }
    }

    public class CompleteRollResponse : BaseResponse
    {
        public bool ParentOrderComplete { get; set; } = false;
    }

    public class UpdateRollRequest : BaseRequest
    {
        public Guid RollId { get; set; }
        public RollStatus Status { get; set; }
    }

    public class UpdateRollResponse : BaseResponse
    {

    }
}