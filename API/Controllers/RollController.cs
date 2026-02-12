using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Threading.Tasks;
using API.Models.RequestsResponses;
using Libs.Enums;
using Libs.Repositories;
using Libs.Services;
using Libs.Services.ScannerStrategies;
using Libs.Services.SP500Export;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class RollController : ControllerBase
    {
        private readonly RollRepository _rollRepository;
        private readonly OrderRepository _orderRepository;
        private readonly FileSystemWatcherService _watcherService;
        private readonly SP500ExporterService _exporterService;

        public RollController(
            RollRepository rollRepository,
            OrderRepository orderRepository,
            FileSystemWatcherService watcherService,
            SP500ExporterService exporterService)
        {
            _rollRepository = rollRepository;
            _orderRepository = orderRepository;
            _watcherService = watcherService;
            _exporterService = exporterService;

            // Wire auto-processing callback for Noritsu scanners
            _watcherService.OnAutoProcessRoll = async (rollId, staffId) =>
            {
                await _rollRepository.ProcessRoll(rollId, staffId);
            };
        }

        [HttpPost("add")]
        public async Task<IActionResult> AddRoll(AddRollRequest req)
        {
            try
            {
                var resp = await _rollRepository.AddRoll(req.OrderId, req.RollNumber);

                if (!resp.IsSuccess)
                    return BadRequest(resp.Message);
                    
                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost("complete")]
        public async Task<IActionResult> ProcessRoll(CompleteRollRequest req)
        {
            try
            {
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

                Guid.TryParse(userIdClaim, out var staffId);
                
                var roll = await _rollRepository.GetRoll(req.RollId);

                if (roll == null)
                    return BadRequest($"Roll ID: {req.RollId} not found");

                var resp = await _rollRepository.ProcessRoll(req.RollId, staffId);

                if (!resp.IsSuccess)
                    return BadRequest(resp.Message);

                var processedRollsResp = await _rollRepository.AllRollsProcessed(roll.Order);

                if(!processedRollsResp.IsSuccess)
                    return BadRequest(processedRollsResp.Message);
                
                var orderComplete = (bool)processedRollsResp.ReturnObject;

                if (orderComplete)
                {
                    var completeOrderResponse = await _orderRepository.UpdateOrderStatus(roll.Order, Libs.Enums.OrderStatus.Completed, staffId);
                    
                    if (!completeOrderResponse.IsSuccess)
                        return BadRequest($"[ERROR]: Error marking order as completed. {Environment.NewLine} {completeOrderResponse.Message}");
                }
                return Ok(new CompleteRollResponse
                    {
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
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

                Guid.TryParse(userIdClaim, out var staffId);

                var roll = await _rollRepository.GetRoll(req.RollId);

                if (roll == null)
                    return BadRequest(new UpdateRollResponse {
                        Success = false,
                        Message = "Error retrieving roll"
                    });

                var resp = await _rollRepository.UpdateRollStatus(roll, req.Status, staffId);

                if(!resp.IsSuccess)
                    return BadRequest(resp.Message);

                // Handle watcher/export lifecycle based on status and scanner profile
                HandleAutomationLifecycle(roll, req.Status, staffId);

                return Ok("Roll status successfully updated");
            }
            catch (ArgumentException ex){
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Manages automation lifecycle (FileSystemWatcher / SP500Exporter) based on roll status changes.
        /// - TimeBasedDelay (Noritsu): auto-starts watcher on ScanningInProgress
        /// - ExitFile (SP-500 Auto): user starts manually via SP500ExportController; cleanup on pause/reset
        /// - Manual (SP-500/SP-3000): no automation
        /// </summary>
        private void HandleAutomationLifecycle(Libs.Data.Models.Roll roll, RollStatus newStatus, Guid staffId)
        {
            if (newStatus == RollStatus.ScanningInProgress)
            {
                // Only start automation for TimeBasedDelay (Noritsu) scanners
                if (roll.Order?.Scanner?.Profile != null)
                {
                    var strategy = ScannerStrategyFactory.CreateStrategy(roll.Order.Scanner);

                    if (strategy?.CompletionMode == CompletionDetectionMode.TimeBasedDelay)
                    {
                        _watcherService.StartWatcherForRoll(roll, staffId);
                    }
                    // ExitFile (SP-500 Auto): no auto-start — user initiates via SP500ExportController
                    // Manual: no action
                }
            }
            else if (newStatus == RollStatus.ScanningPaused || newStatus == RollStatus.Created)
            {
                // Clean up any active automation regardless of scanner type
                _watcherService.StopWatcherForRoll(roll.RollId);
                _exporterService.StopExport(roll.RollId);
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