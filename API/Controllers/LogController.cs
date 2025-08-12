using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using API.Models.RequestsResponses;
using Libs.Data.Models;
using Libs.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LogController : BaseController
    {
        private readonly LogRepository _logRepository;
        // private readonly ILogger<OrderController> _logger;
        private readonly Serilog.ILogger _logger;

        public LogController(LogRepository logRepository, Serilog.ILogger logger)
        {
            _logRepository = logRepository;
            _logger = logger
                .ForContext<OrderController>()
                .ForContext("Area", "Logs");
        }

        [HttpPost("logs")]
        public async Task<IActionResult> GetLogs(GetLogsRequest req)
        {
            try
            {
                _logger.Information("Logs queried at {Time}", DateTime.UtcNow);

                var logsResp = await _logRepository.GetLogs(req.level, req.area, req.page, req.pageSize);
                // var options = new JsonSerializerOptions
                // {
                //     ReferenceHandler = ReferenceHandler.Preserve,
                //     WriteIndented = true
                // };

                if (logsResp.IsSuccess)
                {
                    // var logs = (List<LogEntry>)logsResp.ReturnObject;
                    return Ok(logsResp.ReturnObject);
                    // return Ok(logs);
                }
                else
                {
                    _logger.Error(((Exception)logsResp.ReturnObject).Message);

                    return StatusCode(500, ((Exception)logsResp.ReturnObject).Message);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }

        }
    }
}