using buduns_server.Application.Abstractions.Services;
using buduns_server.Application.UnitOfWork;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace buduns_server.Infrastructure.Services.Mail
{
    public class MailService : IMailService
    {
        private readonly IConfiguration _configuration;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<MailService> _logger;

        public MailService(IConfiguration configuration, IUnitOfWork unitOfWork, ILogger<MailService> logger)
        {
            _configuration = configuration;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public Task SendMailAsync(string to, string subject, string content) => SendMailAsync(new[] { to }, subject, content);

        // Sablonun eksik olmasi istemci hatasi degil sunucu yapilandirma hatasi;
        // bu yuzden NotFoundException degil InvalidOperationException.
        private async Task<string> GetTemplateAsync(string name)
        {
            var utility = await _unitOfWork.UtilityRepository.GetByNameAsync(name);
            if (utility == null)
            {
                throw new InvalidOperationException($"'{name}' mail sablonu veritabaninda bulunamadi.");
            }

            return utility.Value;
        }

        public async Task SendMailAsync(string[] toes, string subject, string content)
        {
            var username = _configuration["Mail:Username"];
            var password = _configuration["Mail:Password"];
            var host = _configuration["Mail:Host"];
            var port = _configuration.GetValue<int>("Mail:Port");
            var fromName = _configuration["Mail:FromName"] ?? "Buduns";

            using var mail = new MailMessage
            {
                IsBodyHtml = true,
                Subject = subject,
                Body = content,
                From = new MailAddress(username!, fromName, Encoding.UTF8)
            };

            foreach (var to in toes)
            {
                mail.To.Add(to);
            }
                
            using var smtp = new SmtpClient(host, port)
            {
                EnableSsl = true,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(username, password),
                DeliveryMethod = SmtpDeliveryMethod.Network
            };

            try
            {
                await smtp.SendMailAsync(mail);

                _logger.LogInformation(
                    "Mail sent successfully. Subject: {Subject}, RecipientCount: {RecipientCount}, Host: {Host}",
                    subject,
                    toes.Length,
                    host);
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Mail sending failed. Subject: {Subject}, RecipientCount: {RecipientCount}, Host: {Host}",
                    subject,
                    toes.Length,
                    host);

                throw;
            }
        }

        // Şifre Sıfırlama Maili
        public async Task SendForgotPasswordMailAsync(string to, string fullName, string verificationCode)
        {
            string description = await GetTemplateAsync("FORGOT_PASSWORD");
            description = description.Replace("{full_name}", $"{fullName}");
            description = description.Replace("{verification_code}", verificationCode);
            description = description.Replace("{reset_code}", verificationCode);
            description = description.Replace("{reset_link}", verificationCode);
            description = description.Replace("{app_name}", "Buduns");

            await SendMailAsync(to, "Şifre Sıfırlama Talebi", description);
        }

        // Mail Doğrulama
        public async Task SendVerifyMailAsync(string to, string fullName, string verificationCode)
        {
            string description = await GetTemplateAsync("MAIL_VERIFY");
            description = description.Replace("{full_name}", $"{fullName}");
            description = description.Replace("{verification_code}", verificationCode);
            description = description.Replace("{verify_code}", verificationCode);
            description = description.Replace("{verify_link}", verificationCode);
            description = description.Replace("{app_name}", "Buduns");

            await SendMailAsync(to, "E-Posta Doğrulama", description);
        }

        // Mevcut Email Değiştirme Onayı
        public async Task SendChangeEmailOldMailAsync(string to, string fullName, string newEmail, string verificationCode)
        {
            string description = await GetTemplateAsync("CHANGE_EMAIL_OLD");
            description = description.Replace("{full_name}", $"{fullName}");
            description = description.Replace("{new_email}", newEmail);
            description = description.Replace("{verification_code}", verificationCode);
            description = description.Replace("{confirm_code}", verificationCode);
            description = description.Replace("{confirm_link}", verificationCode);
            description = description.Replace("{app_name}", "Buduns");

            await SendMailAsync(to, "E-Posta Değişikliği Onayı", description);
        }

        // Email Değiştirme
        public async Task SendChangeEmailMailAsync(string to, string fullName, string verificationCode)
        {
            string description = await GetTemplateAsync("CHANGE_EMAIL");
            description = description.Replace("{full_name}", $"{fullName}");
            description = description.Replace("{verification_code}", verificationCode);
            description = description.Replace("{confirm_code}", verificationCode);
            description = description.Replace("{confirm_link}", verificationCode);
            description = description.Replace("{app_name}", "Buduns");

            await SendMailAsync(to, "E-Posta Değişikliği Talebi", description);
        }

    }
}
