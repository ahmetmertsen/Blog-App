using blogapp_server.Application.Abstractions.Services;
using blogapp_server.Application.UnitOfWork;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Infrastructure.Services.Mail
{
    public class MailService : IMailService
    {
        private readonly IConfiguration _configuration;
        private readonly IUnitOfWork _unitOfWork;

        public MailService(IConfiguration configuration, IUnitOfWork unitOfWork)
        {
            _configuration = configuration;
            _unitOfWork = unitOfWork;
        }

        public Task SendMailAsync(string to, string subject, string content)
            => SendMailAsync(new[] { to }, subject, content);

        public async Task SendMailAsync(string[] toes, string subject, string content)
        {
            var username = _configuration["Mail:Username"];
            var password = _configuration["Mail:Password"];
            var host = _configuration["Mail:Host"];
            var port = _configuration.GetValue<int>("Mail:Port");
            var fromName = _configuration["Mail:FromName"] ?? "BlogApp";

            using var mail = new MailMessage
            {
                IsBodyHtml = true,
                Subject = subject,
                Body = content,
                From = new MailAddress(username!, fromName, Encoding.UTF8)
            };

            foreach (var to in toes)
                mail.To.Add(to);

            using var smtp = new SmtpClient(host, port)
            {
                EnableSsl = true,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(username, password),
                DeliveryMethod = SmtpDeliveryMethod.Network
            };

            await smtp.SendMailAsync(mail);
        }

        // Şifre Sıfırlama Maili
        public async Task SendForgotPasswordMailAsync(string to, string fullName, int userId, string resetToken)
        {
            var utilityResponse = await _unitOfWork.UtilityRepository.GetByNameAsync("FORGOT_PASSWORD");
            string description = utilityResponse.Value;
            description = description.Replace("{full_name}", $"{fullName}");
            description = description.Replace("{reset_link}", $"https://www.google.com/update-password/{userId}/{resetToken}");
            description = description.Replace("{app_name}", "BlogApp");

            await SendMailAsync(to, "Şifre Sıfırlama Talebi", description);
        }

        // Mail Doğrulama
        public async Task SendVerifyMailAsync(string to, string fullName, int userId, string emailConfirmToken)
        {
            var utilityResponse = await _unitOfWork.UtilityRepository.GetByNameAsync("MAIL_VERIFY");
            string description = utilityResponse.Value;
            description = description.Replace("{full_name}", $"{fullName}");
            description = description.Replace("{verify_link}", $"https://www.google.com/verify-email/{userId}/{emailConfirmToken}");
            description = description.Replace("{app_name}", "BlogApp");

            await SendMailAsync(to, "E-Posta Doğrulama", description);
        }

        // Email Değiştirme
        public async Task SendChangeEmailMailAsync(string to, string fullName, int userId, string emailChangeToken)
        {
            var utilityResponse = await _unitOfWork.UtilityRepository.GetByNameAsync("CHANGE_EMAIL");
            string description = utilityResponse.Value;
            description = description.Replace("{full_name}", $"{fullName}");
            description = description.Replace("{confirm_link}", $"https://www.google.com/change-email/{userId}/{emailChangeToken}");
            description = description.Replace("{app_name}", "BlogApp");

            await SendMailAsync(to, "E-Posta Değişikliği Talebi", description);
        }
    }
}
