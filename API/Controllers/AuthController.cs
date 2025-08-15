using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Libs.Repositories;
using Microsoft.AspNetCore.Mvc;
using API.Models.RequestsResponses;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Libs.Data.Models;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Libs.Classes;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : BaseController
    {
        private readonly Serilog.ILogger _logger;
        private readonly AuthRepository _authRepository;
        private readonly UserManager<Staff> _userManager;
        private readonly IConfiguration _config;

        public AuthController(Serilog.ILogger logger, AuthRepository authRepository,
            UserManager<Staff> userManager, IConfiguration config)
        {
            _userManager = userManager;
            _config = config;
            _authRepository = authRepository;
            _logger = logger
                .ForContext<AuthController>()
                .ForContext("Area", "Authentication");
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterStaffRequest req)
        {
            try
            {
                var result = await _authRepository.Register(
                    req.UserName,
                    req.Email,
                    req.Password,
                    req.FirstName,
                    req.LastName
                );
                if (!result.IsSuccess)
                {
                    _logger.Error(result.Message);
                    return StatusCode(500, result.Message);
                }

                return Ok("Staff registered");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, ex.Message);
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            try
            {
                var user = await _userManager.FindByNameAsync(request.Username);
                if (user == null || !await _userManager.CheckPasswordAsync(user, request.Password))
                    return Unauthorized("Invalid username or password");

                var tokenResp = await GenerateJwtToken(user);
                if (!tokenResp.IsSuccess)
                {
                    _logger.Error((Exception)tokenResp.ReturnObject, tokenResp.Message);
                    return StatusCode(500, tokenResp.Message);
                }
                return Ok(new LoginResponse
                {
                    Success = true,
                    Token = (string)tokenResp.ReturnObject,
                    Expiration = DateTime.UtcNow.AddMinutes(Convert.ToDouble(_config["Jwt:ExpiresInMinutes"]))
                });
            }
            catch (Exception ex)
            {
                _logger.Error(ex, ex.Message);
                return StatusCode(500, ex.Message);
            }
        }

        private async Task<SystemResponse> GenerateJwtToken(Staff user)
        {
            try
            {
                var jwtKey = _config["Jwt:Key"];
                var jwtIssuer = _config["Jwt:Issuer"];

                var userClaims = await _userManager.GetClaimsAsync(user);
                var roles = await _userManager.GetRolesAsync(user);

                var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

                claims.AddRange(userClaims);
                claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                var token = new JwtSecurityToken(
                    issuer: jwtIssuer,
                    audience: _config["Jwt:Audience"],
                    claims: claims,
                    expires: DateTime.UtcNow.AddMinutes(Convert.ToDouble(_config["Jwt:ExpiresInMinutes"])),
                    signingCredentials: creds
                );

                return new SystemResponse
                {
                    IsSuccess = true,
                    ReturnObject = new JwtSecurityTokenHandler().WriteToken(token)
                };
            }
            catch (Exception ex)
            {
                return new SystemResponse
                {
                    IsSuccess = false,
                    Message = ex.Message,
                    ReturnObject = ex
                };
            }
        }
    }
}