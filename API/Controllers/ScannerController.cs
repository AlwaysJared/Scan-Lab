using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using API.Models;
using Libs.Data.Context;
using Libs.Data.Models;
using Libs.Services;
using Libs.Repositories;
using API.Models.RequestsResponses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;

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

        [Authorize]
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
            return Ok($"Scanner sucessfully deleted");
        }

        [HttpGet("scanners")]
        public async Task<IActionResult> GetScanners()
        {
            try{
                var scanners = await _scannerRepository.GetScanners();
                return Ok(scanners);
            }
            catch(Exception ex){
                return BadRequest(ex.Message);
            }
            
        }

        [HttpPost("update")]
        public async Task<IActionResult> UpdateScanner(UpdateScannerRequest req)
        {
            try{
                var resp = await _scannerRepository.UpdateScanner(req.Scnr);

                if(!resp.IsSuccess){
                    return BadRequest(resp.Message);
                }

                return Ok();
            }
            catch(Exception ex){
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("profiles")]
        public async Task<IActionResult> GetProfiles()
        {
            try
            {
                var profiles = await _scannerRepository.GetProfiles();
                return Ok(profiles);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("profile-configurations/{profileId}")]
        public async Task<IActionResult> GetProfileConfigurations(Guid profileId)
        {
            try
            {
                var configs = await _scannerRepository.GetProfileConfigurations(profileId);
                return Ok(configs);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [Authorize]
        [HttpPost("update-profile-configuration")]
        public async Task<IActionResult> UpdateProfileConfiguration(UpdateProfileConfigRequest req)
        {
            try
            {
                var resp = await _scannerRepository.UpdateProfileConfiguration(req.ConfigId, req.ConfigValue);

                if (!resp.IsSuccess)
                    return BadRequest(resp.Message);

                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}