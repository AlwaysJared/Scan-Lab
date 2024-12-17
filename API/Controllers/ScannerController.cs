using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using API.Models;
using Libs.Data.Context;
using Libs.Data.Models;
using Libs.Services;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ScannerController : ControllerBase
    {
        private ScannerRepository _scannerRepository;
        public ScannerController(ScannerRepository scannerRepository)
        {
            _scannerRepository = scannerRepository;
        }

        [HttpPost("add")]
        public async Task<IActionResult> AddScanner(AddScannerRequest req)
        {
            try
            {
                var resp = await _scannerRepository.AddScanner(req);
                return Ok(resp);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new SubmitOrderResponse{Message = ex.Message});
            }
        }

        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> DeleteScanner(Guid id)
        {
            var resp = await _scannerRepository.DeleteScanner(id);
            if (!resp.IsSuccess){
                return BadRequest(new DeleteScannerResponse{
                    Message = resp.Message
                });
            }
            return Ok($"Watcher disposed for ID {id}");
        }

        [HttpGet("scanners")]
        public async Task<IActionResult> GetScanners()
        {
            var scanners = await _scannerRepository.GetScanners();
            return Ok(scanners);
        }
    }
}