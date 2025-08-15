using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Libs.Classes;

namespace Libs.Interfaces
{
    public interface IAuthRepository : IDisposable
    {
        Task<SystemResponse> Register(string username, string? email, string password, string firstName, string lastName);
    }
}