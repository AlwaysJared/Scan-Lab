using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Libs.Classes;
using Libs.Data.Context;
using Libs.Data.Models;
using Libs.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Libs.Repositories
{
    public class AuthRepository : IAuthRepository, IDisposable
    {
        private ScanLabContext context;
        private readonly UserManager<Staff> _userManager;

        public AuthRepository(ScanLabContext context, UserManager<Staff> userManager)
        {
            this.context = context;
            _userManager = userManager;
        }

        public void Dispose()
        {
            context.Dispose();
        }

        public async Task<SystemResponse> Register(string username, string? email, string password, string firstName, string lastName)
        {
            try
            {
                var newStaff = new Staff
                {
                    FirstName = firstName,
                    LastName = lastName,
                    UserName = username,
                    Email = email,
                };

                var result = await _userManager.CreateAsync(newStaff, password);

                if (!result.Succeeded)
                    return new SystemResponse
                    {
                        IsSuccess = false,
                        Message = result.Errors.ToString(),
                        ReturnObject = result.Errors
                    };

                return new SystemResponse
                {
                    IsSuccess = true,
                    ReturnObject = newStaff.Id.ToString()
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

        public async Task<SystemResponse> ResetPassword(string staffId, string token, string newPassword, string confirmPassword)
        {
            try
            {
                if (newPassword != confirmPassword)
                    return new SystemResponse
                    {
                        IsSuccess = false,
                        Message = "Staff not provided",
                    };

                var staff = await _userManager.FindByIdAsync(staffId);
                if (staff == null)
                    return new SystemResponse
                    {
                        IsSuccess = false,
                        Message = "Staff not provided",
                    };

                if (context.Staff.Where(s => s.Id == staff.Id).FirstOrDefault() == null)
                    return new SystemResponse
                    {
                        IsSuccess = false,
                        Message = $"Staff id '{staff.Id}' not found"
                    };

                var result = await _userManager.ResetPasswordAsync(staff, token, newPassword);

                return new SystemResponse
                {
                    IsSuccess = true,
                    ReturnObject = result
                };
            }
            catch (Exception ex)
            {
                return new SystemResponse
                {
                    IsSuccess = false,
                    IsError = true,
                    ReturnObject = ex
                };
            }
        }

        public async Task<SystemResponse> GetResetPasswordToken(string staffId)
        {
            try
            {
                var staff = await _userManager.FindByIdAsync(staffId);
                if (staff == null)
                    return new SystemResponse
                    {
                        IsSuccess = false,
                        Message = "Staff not provided",
                    };

                var token = await _userManager.GeneratePasswordResetTokenAsync(staff);

                if (token == null)
                    return new SystemResponse
                    {
                        IsSuccess = false,
                        Message = "Error when creating token"
                    };

                return new SystemResponse
                    {
                        IsSuccess = true,
                        ReturnObject = token
                    };
            }
            catch (Exception ex)
            {
                return new SystemResponse
                {
                    IsSuccess = false,
                    IsError = true,
                    ReturnObject = ex
                };
            }
        }
    }
}