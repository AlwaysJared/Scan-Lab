using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Libs.Data.RequestResponse.Staff;
using Libs.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StaffController : ControllerBase
    {
        private StaffRepository _staffRepository;
        private readonly Serilog.ILogger _logger;
        public StaffController(StaffRepository staffRepository, Serilog.ILogger logger)
        {
            _staffRepository = staffRepository;
            _logger = logger
                .ForContext<StaffController>()
                .ForContext("Area", "Staff");
        }

        [HttpPost("staff")]
        public async Task<IActionResult> GetStaff(GetStaffRequest req)
        {
            try
            {
                _logger.Information("Staff queried at {Time}", DateTime.UtcNow);

                var staffResp = await _staffRepository.GetStaff(req.StaffId, req.Page, req.PageSize);
                if (!staffResp.IsSuccess)
                {
                    _logger.Error((Exception)staffResp.ReturnObject, $"Error retrieving staff: {(Exception)staffResp.ReturnObject}");
                    return StatusCode(500, $"Error retrieving staff: {(Exception)staffResp.ReturnObject}");
                }
                // var options = new JsonSerializerOptions
                // {
                //     ReferenceHandler = ReferenceHandler.Preserve,
                //     WriteIndented = true
                // };

                // string json = JsonSerializer.Serialize(staffResp.ReturnObject, options);

                // return Ok(staffResp.ReturnObject);
                return Ok(staffResp.ReturnObject);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, ex.Message);
                return StatusCode(500, ex.Message);
            }
        }
    }
}