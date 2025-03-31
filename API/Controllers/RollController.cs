using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using API.Models.RequestsResponses;
using Libs.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RollController : ControllerBase
    {
        private readonly RollRepository _rollRepository;

        public RollController(RollRepository rollRepository)
        {
            _rollRepository = rollRepository;
        }

        [HttpPost("complete")]
        public async Task<IActionResult> ProcessRoll(CompleteRollRequest req)
        {
            try
            {
                var roll = await _rollRepository.GetRoll(req.RollId);

                if (roll == null)
                    return BadRequest($"Roll ID: {req.RollId} not found");

                var resp = await _rollRepository.ProcessRoll(req.RollId);

                if (!resp.IsSuccess)
                    return BadRequest(resp.Message);

                var processedRollsResp = _rollRepository.AllRollsProcessed(roll.Order);

                if(!processedRollsResp.IsSuccess)
                    return BadRequest(processedRollsResp.Message);
                
                var orderComplete = (bool)processedRollsResp.ReturnObject;

                return Ok(new CompleteRollResponse{
                    Success = true,
                    ParentOrderComplete = orderComplete
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new CompleteRollResponse{Message = ex.Message});
            }
        }

        [HttpPut("updateStatus")]
        public async Task<IActionResult> UpdateRollStatus(UpdateRollRequest req)
        {
            try{
                var roll = await _rollRepository.GetRoll(req.RollId);

                if (roll == null)
                    return BadRequest(new UpdateRollResponse {
                        Success = false,
                        Message = "Error retrieving roll"
                    });
                
                var resp = await _rollRepository.UpdateRollStatus(roll, req.Status);

                if(!resp.IsSuccess)
                    return BadRequest(resp.Message);
                
                return Ok("Roll status successfully updated");
            }
            catch (ArgumentException ex){
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("delete")]
        public async Task<IActionResult> DeleteRoll(DeleteRollRequest req)
        {
            try{
                // var roll = await _rollRepository.GetRoll(req.RollId);

                // if (roll == null)
                //     return BadRequest(new UpdateRollResponse {
                //         Success = false,
                //         Message = "Error retrieving roll"
                //     });
                
                var resp = await _rollRepository.DeleteRoll(req.RollId);

                if(!resp.IsSuccess)
                    return BadRequest(resp.Message);
                
                return Ok("Roll successfully deleted");
            }
            catch (ArgumentException ex){
                return BadRequest(ex.Message);
            }
        }
    }
}