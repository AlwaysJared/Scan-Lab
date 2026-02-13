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
    /// <summary>
    /// API controller for scanner profile CRUD operations and strategy management.
    /// </summary>
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

        /// <summary>
        /// Gets all active scanner profiles.
        /// </summary>
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

        /// <summary>
        /// Gets a single scanner profile by ID.
        /// </summary>
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

        /// <summary>
        /// Creates a new scanner profile with strategy validation.
        /// </summary>
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

        /// <summary>
        /// Updates an existing scanner profile.
        /// </summary>
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

        /// <summary>
        /// Soft-deletes a scanner profile. Returns 400 if scanners are using it.
        /// </summary>
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

        /// <summary>
        /// Gets all registered strategy class names from the factory.
        /// </summary>
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

        /// <summary>
        /// Gets all configuration entries for a specific profile.
        /// </summary>
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

        /// <summary>
        /// Updates a single profile configuration value.
        /// </summary>
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
