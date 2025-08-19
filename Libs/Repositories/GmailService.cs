using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Libs.Classes;
using Libs.Data.Context;
using Libs.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.EntityFrameworkCore;
using MimeKit;

namespace Libs.Repositories
{
    public class GmailService : IEmailService
    {
        private readonly ScanLabContext _context;
        private string _fromEmail;
        private string _appPassword; //Google App Password

        //  string fromEmail, string appPassword
        public GmailService(ScanLabContext context)
        {
            _context = context;
            // _fromEmail = fromEmail;
            // _appPassword = appPassword;
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        public async Task<SystemResponse> SendEmailAsync(string to, string subject, string body)
        {
            try
            {
                var emailConfig = await _context.ConfigSettings.Where(c => c.Key.ToLower() == "emailsettings").FirstOrDefaultAsync();

                if (emailConfig == null)
                    return new SystemResponse
                    {
                        IsSuccess = false,
                        Message = "Email configuration not found. Please setup to send emails"
                    };

                _fromEmail = JsonSerializer.Deserialize<EmailConfig>(emailConfig.Value).Email;
                _appPassword = JsonSerializer.Deserialize<EmailConfig>(emailConfig.Value).Password;

                

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("ScanLab", _fromEmail));
                message.To.Add(MailboxAddress.Parse(to));
                message.Subject = subject;

                // message.Body = new TextPart("plain")
                // {
                //     Text = body
                // };
                var bodyBuilder = new MimeKit.BodyBuilder
                {
                    HtmlBody = body
                };
                message.Body = bodyBuilder.ToMessageBody();

                using var client = new SmtpClient();
                await client.ConnectAsync("smtp.gmail.com", 587, SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(_fromEmail, _appPassword);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                return new SystemResponse
                {
                    IsSuccess = true
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