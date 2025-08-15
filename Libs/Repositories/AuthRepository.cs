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
                    ReturnObject = newStaff.Id
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