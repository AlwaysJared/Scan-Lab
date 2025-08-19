using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Libs.Classes;

namespace Libs.Interfaces
{
    public interface IEmailService : IDisposable
    {
        Task<SystemResponse> SendEmailAsync(string to, string subject, string body);
    }
}