using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Libs.Services.SP500Export;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class SP500ExportController : ControllerBase
    {
        private readonly SP500ExporterService _exporterService;

        public SP500ExportController(SP500ExporterService exporterService)
        {
            _exporterService = exporterService;
        }

        [HttpPost("start/{rollId}")]
        public async Task<IActionResult> StartExport(Guid rollId)
        {
            try
            {
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                Guid.TryParse(userIdClaim, out var staffId);

                var result = await _exporterService.StartExport(rollId, staffId);

                if (!result.IsSuccess)
                    return BadRequest(result.Message);

                return Ok(new { SessionId = result.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost("stop/{rollId}")]
        public IActionResult StopExport(Guid rollId)
        {
            try
            {
                var result = _exporterService.StopExport(rollId);

                if (!result.IsSuccess)
                    return BadRequest(result.Message);

                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("status/{rollId}")]
        public IActionResult GetStatus(Guid rollId)
        {
            try
            {
                var status = _exporterService.GetSessionStatus(rollId);

                if (status == null)
                    return NotFound("No active export session for this roll");

                return Ok(status);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("sessions")]
        public IActionResult GetActiveSessions()
        {
            try
            {
                var sessions = _exporterService.GetActiveSessions();
                return Ok(sessions);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}
