using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Libs.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using API.Models.RequestsResponses.Analytics;

namespace API.Controllers
{
    [ApiController]
    // [Authorize]
    [Route("api/[controller]")]
    public class AnalyticsController : ControllerBase
    {
        private readonly AnalyticsRepository _analyticsRepository;
        private readonly Serilog.ILogger _logger;

        public AnalyticsController(AnalyticsRepository analyticsRepository,
            Serilog.ILogger logger
        )
        {
            _analyticsRepository = analyticsRepository;
            _logger = logger
                .ForContext<AnalyticsController>()
                .ForContext("Area", "Analytics");
        }

        [HttpPost("RollsPerStaff")]
        public async Task<IActionResult> RollsPerStaff(AnalyticsRequest req)
        {
            try
            {
                var resp = await _analyticsRepository.RollsPerStaffInTimeframe(
                    req.Ids,
                    req.StartDate,
                    req.EndDate,
                    req.IsAverage
                );

                if (!resp.IsSuccess)
                    return StatusCode(500, resp.Message);
                    
                return Ok(resp.ReturnObject);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.ToString());
            }
        }
    }
}