using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using API.Models;
using Libs.Data.Context;
using Libs.Data.Models;
using Libs.Services;
using Libs.Repositories;

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
                var newScnr = new Scanner
                {
                    Id = Guid.NewGuid(),
                    ScannerName = req.ScannerName,
                    Make = req.Make,
                    Model = req.Model,
                    WatchedDir = req.WatchedDir,
                    DestinationDir = req.DestinationDir,
                    ArchiveDir = req.ArchiveDir,
                    ArtistName = req.ArtistName
                };

                var resp = await _scannerRepository.AddScanner(newScnr);
                
                return Ok(resp);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new SubmitOrderResponse
                {
                    Message = ex.Message
                });
            }
        }

        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> DeleteScanner(Guid id)
        {
            var resp = await _scannerRepository.DeleteScanner(id);
            if (!resp.IsSuccess)
            {
                return BadRequest(new DeleteScannerResponse
                {
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