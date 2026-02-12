using System;
using System.Threading.Tasks;
using API.Models.RequestsResponses;
using Libs.Data.Models;
using Libs.Repositories;
using Libs.Services.ScannerStrategies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class ScannerProfileController : ControllerBase
    {
        private readonly ProfileRepository _profileRepository;

        public ScannerProfileController(ProfileRepository profileRepository)
        {
            _profileRepository = profileRepository;
        }

        [HttpGet("profiles")]
        public async Task<IActionResult> GetProfiles()
        {
            try
            {
                var profiles = await _profileRepository.GetProfiles();
                return Ok(profiles);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("profile/{id}")]
        public async Task<IActionResult> GetProfile(Guid id)
        {
            try
            {
                var profile = await _profileRepository.GetProfile(id);

                if (profile == null)
                    return NotFound("Profile not found");

                return Ok(profile);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost("add")]
        public async Task<IActionResult> AddProfile(AddProfileRequest req)
        {
            try
            {
                var profile = new ScannerProfile
                {
                    ProfileName = req.ProfileName,
                    StrategyClassName = req.StrategyClassName,
                    Description = req.Description
                };

                var resp = await _profileRepository.AddProfile(profile);

                if (!resp.IsSuccess)
                    return BadRequest(resp.Message);

                return Ok(resp.ReturnObject);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPut("update")]
        public async Task<IActionResult> UpdateProfile(UpdateProfileRequest req)
        {
            try
            {
                var profile = new ScannerProfile
                {
                    Id = req.Id,
                    ProfileName = req.ProfileName,
                    StrategyClassName = req.StrategyClassName,
                    Description = req.Description
                };

                var resp = await _profileRepository.UpdateProfile(profile);

                if (!resp.IsSuccess)
                    return BadRequest(resp.Message);

                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> DeleteProfile(Guid id)
        {
            try
            {
                var resp = await _profileRepository.DeleteProfile(id);

                if (!resp.IsSuccess)
                    return BadRequest(resp.Message);

                return Ok("Profile deleted successfully");
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("strategies")]
        public IActionResult GetAvailableStrategies()
        {
            try
            {
                var strategies = ScannerStrategyFactory.GetAvailableStrategies();
                return Ok(strategies);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("profile/{id}/configurations")]
        public async Task<IActionResult> GetProfileConfigurations(Guid id)
        {
            try
            {
                var configs = await _profileRepository.GetProfileConfigurations(id);
                return Ok(configs);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost("update-configuration")]
        public async Task<IActionResult> UpdateProfileConfiguration(UpdateProfileConfigRequest req)
        {
            try
            {
                var config = new ProfileConfiguration
                {
                    Id = req.ConfigId,
                    ConfigKey = string.Empty,
                    ConfigValue = req.ConfigValue
                };

                var resp = await _profileRepository.UpdateProfileConfiguration(config);

                if (!resp.IsSuccess)
                    return BadRequest(resp.Message);

                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}
