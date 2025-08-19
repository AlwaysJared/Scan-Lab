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
        private readonly GmailService _gmailService;

        public AuthController(Serilog.ILogger logger, AuthRepository authRepository,
            UserManager<Staff> userManager, IConfiguration config, GmailService gmailService)
        {
            _userManager = userManager;
            _config = config;
            _authRepository = authRepository;
            _gmailService = gmailService;
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

                if (!String.IsNullOrEmpty(req.Email.Trim()))
                {
                    var tokenResp = await _authRepository.GetResetPasswordToken((string)result.ReturnObject);
                    if (!tokenResp.IsSuccess)
                    {
                        return Ok($"Staff registered. However, there was an error generating password reset token.\n{tokenResp.Message}\n Please reset staff password manually.");
                    }
                    var resetUrl = $"{_config["AppSettings:BaseUrl"]}/api/auth/password-reset?userId={(string)result.ReturnObject}&token={Uri.EscapeDataString((string)tokenResp.ReturnObject)}";
                    string subject = "ScanLab - [Action Required]: Set Password";
                    string body = $@"
                    <p>Your staff account was successfully created.</p>
                    <p>Click <a href='{resetUrl}'>here</a> to set your password.</p>";
                    var resetEmailResult = await _gmailService.SendEmailAsync(req.Email, subject, body);
                    if (!resetEmailResult.IsSuccess)
                    {
                        return Ok($"Staff registered. However, there sending the password. \n{resetEmailResult.Message}\n Please reset staff password manually");
                    }
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

        [HttpGet("password-reset")]
        public IActionResult ShowPasswordResetPage([FromQuery] string userId, [FromQuery] string token)
        {
            // You could validate params minimally here

            var html = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <title>Password Reset</title>
                    <meta charset='utf-8'/>
                    <style>
                        body {{
                            font-family: Arial, sans-serif;
                            display: flex;
                            justify-content: center;
                            align-items: center;
                            height: 100vh;
                            background: #f4f4f4;
                        }}
                        .reset-container {{
                            background: #fff;
                            padding: 2rem;
                            border-radius: 8px;
                            box-shadow: 0 2px 8px rgba(0,0,0,0.1);
                            width: 300px;
                            text-align: center;
                        }}
                        input {{
                            width: 100%;
                            padding: 10px;
                            margin: 8px 0;
                            border: 1px solid #ccc;
                            border-radius: 4px;
                        }}
                        button {{
                            width: 100%;
                            padding: 10px;
                            background: #007BFF;
                            color: white;
                            border: none;
                            border-radius: 4px;
                            cursor: pointer;
                        }}
                        button:hover {{
                            background: #0056b3;
                        }}
                    </style>
                </head>
                <body>
                    <div class='reset-container'>
                        <h2>Reset Password</h2>
                        <form method='post' action='/api/auth/reset-password'>
                            <input type='hidden' name='userId' value='{userId}' />
                            <input type='hidden' name='token' value='{token}' />
                            <input type='password' name='newPassword' placeholder='New Password' required />
                            <input type='password' name='confirmPassword' placeholder='Confirm Password' required />
                            <button type='submit'>Update Password</button>
                        </form>
                    </div>
                </body>
                </html>";

            return Content(html, "text/html");
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromForm] string userId, [FromForm] string token, [FromForm] string newPassword, [FromForm] string confirmPassword)
        {
            var result = await _authRepository.ResetPassword(userId, token, newPassword, confirmPassword);

            if (!result.IsSuccess)
            {
                var errors = string.Join("<br/>", ((IdentityResult?)result.ReturnObject).Errors.Select(e => e.Description));
                return Content($"<h2>Password reset failed:</h2><p>{errors}</p>", "text/html");
            }

            return Content("<h2>Password reset successful. You may now close this page.</h2>", "text/html");
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