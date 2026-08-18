using buduns_server.Application.Abstractions.Services;
using buduns_server.Application.Common.Consts;
using buduns_server.Application.Common.Options;
using buduns_server.Application.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
        /// <summary>Sablonlardaki {app_name} yer tutucusunun karsiligi.</summary>
        private const string ApplicationName = "Buduns";

        private readonly MailOptions _options;
        private readonly IUtilityRepository _utilityRepository;
        private readonly ILogger<MailService> _logger;

        public MailService(IOptions<MailOptions> options, IUtilityRepository utilityRepository, ILogger<MailService> logger)
        {
            _options = options.Value;
            _utilityRepository = utilityRepository;
            _logger = logger;
        }

        public Task SendMailAsync(string to, string subject, string content) => SendMailAsync(new[] { to }, subject, content);

        /// <summary>
        /// SMTP bilgileri eksikken gonderim denenirse hata System.Net.Mail'in
        /// icinden "The value cannot be an empty string. (Parameter 'address')"
        /// olarak cikiyordu; hangi ayarin eksik oldugu hicbir yerde yazmiyordu.
        /// </summary>
        private void EnsureConfigured()
        {
            if (_options.IsConfigured)
            {
                return;
            }

            throw new InvalidOperationException(
                $"Mail gonderilemez: SMTP yapilandirmasi eksik. Eksik ayarlar: {string.Join(", ", _options.GetMissingSettings())}.");
        }

        // Sablonun eksik olmasi istemci hatasi degil sunucu yapilandirma hatasi;
        // bu yuzden NotFoundException degil InvalidOperationException.
        private async Task<string> GetTemplateAsync(string name)
        {
            var utility = await _utilityRepository.GetByNameAsync(name);
            if (utility == null)
            {
                throw new InvalidOperationException($"'{name}' mail sablonu veritabaninda bulunamadi.");
            }

            return utility.Value;
        }

        public async Task SendMailAsync(string[] toes, string subject, string content)
        {
            EnsureConfigured();

            using var mail = new MailMessage
            {
                IsBodyHtml = true,
                Subject = subject,
                Body = content,
                From = new MailAddress(_options.Username, _options.FromName, Encoding.UTF8)
            };

            foreach (var to in toes)
            {
                mail.To.Add(to);
            }

            using var smtp = new SmtpClient(_options.Host, _options.Port)
            {
                EnableSsl = true,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(_options.Username, _options.Password),
                DeliveryMethod = SmtpDeliveryMethod.Network
            };

            try
            {
                await smtp.SendMailAsync(mail);

                _logger.LogInformation(
                    "Mail sent successfully. Subject: {Subject}, RecipientCount: {RecipientCount}, Host: {Host}",
                    subject,
                    toes.Length,
                    _options.Host);
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Mail sending failed. Subject: {Subject}, RecipientCount: {RecipientCount}, Host: {Host}",
                    subject,
                    toes.Length,
                    _options.Host);

                throw;
            }
        }

        // Şifre Sıfırlama Maili
        public async Task SendForgotPasswordMailAsync(string to, string fullName, string verificationCode)
        {
            var body = await BuildBodyAsync(MailTemplateKeys.ForgotPassword, fullName, verificationCode);

            await SendMailAsync(to, "Şifre Sıfırlama Talebi", body);
        }

        // Mail Doğrulama
        public async Task SendVerifyMailAsync(string to, string fullName, string verificationCode)
        {
            var body = await BuildBodyAsync(MailTemplateKeys.MailVerify, fullName, verificationCode);

            await SendMailAsync(to, "E-Posta Doğrulama", body);
        }

        // Mevcut Email Değiştirme Onayı
        public async Task SendChangeEmailOldMailAsync(string to, string fullName, string newEmail, string verificationCode)
        {
            var body = await BuildBodyAsync(MailTemplateKeys.ChangeEmailOld, fullName, verificationCode);
            body = body.Replace("{new_email}", newEmail);

            await SendMailAsync(to, "E-Posta Değişikliği Onayı", body);
        }

        // Email Değiştirme
        public async Task SendChangeEmailMailAsync(string to, string fullName, string verificationCode)
        {
            var body = await BuildBodyAsync(MailTemplateKeys.ChangeEmail, fullName, verificationCode);

            await SendMailAsync(to, "E-Posta Değişikliği Talebi", body);
        }

        /// <summary>
        /// Tum sablonlarda ortak olan yer tutucular. Sablona ozel olanlar
        /// (ornegin {new_email}) cagiran metotta doldurulur.
        /// </summary>
        private async Task<string> BuildBodyAsync(string templateKey, string fullName, string verificationCode)
        {
            var body = await GetTemplateAsync(templateKey);

            return body
                .Replace("{full_name}", fullName)
                .Replace("{verification_code}", verificationCode)
                .Replace("{app_name}", ApplicationName);
        }
    }
}
