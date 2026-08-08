using System.Collections.Concurrent;
using buduns_server.Application.Abstractions.Services;

namespace buduns_server.IntegrationTests.Fixtures;

public sealed record SentMail(string To, string Purpose, string? VerificationCode);

/// <summary>
/// Dogrulama kodlari yalnizca e-postayla gidiyor ve veritabaninda hash olarak
/// duruyor. Uctan uca akislari (kayit, sifre sifirlama, e-posta degisikligi)
/// test edebilmek icin gonderilen kodlar burada tutuluyor.
/// </summary>
public sealed class TestMailService : IMailService
{
    private readonly ConcurrentQueue<SentMail> _sentMails = new();

    public IReadOnlyList<SentMail> SentMails => _sentMails.ToArray();

    public void Clear() => _sentMails.Clear();

    public string LastVerificationCodeFor(string to, string purpose) =>
        _sentMails.LastOrDefault(mail => mail.To.Equals(to, StringComparison.OrdinalIgnoreCase) && mail.Purpose == purpose)?.VerificationCode
        ?? throw new InvalidOperationException($"'{to}' adresine '{purpose}' amacli dogrulama kodu gonderilmemis. Gonderilenler: {string.Join(", ", _sentMails.Select(mail => $"{mail.To}/{mail.Purpose}"))}");

    public Task SendMailAsync(string to, string subject, string content)
    {
        _sentMails.Enqueue(new SentMail(to, MailPurposes.Generic, null));
        return Task.CompletedTask;
    }

    public Task SendMailAsync(string[] toes, string subject, string content)
    {
        foreach (var to in toes)
        {
            _sentMails.Enqueue(new SentMail(to, MailPurposes.Generic, null));
        }

        return Task.CompletedTask;
    }

    public Task SendForgotPasswordMailAsync(string to, string userFullName, string verificationCode)
    {
        _sentMails.Enqueue(new SentMail(to, MailPurposes.PasswordReset, verificationCode));
        return Task.CompletedTask;
    }

    public Task SendVerifyMailAsync(string to, string fullName, string verificationCode)
    {
        _sentMails.Enqueue(new SentMail(to, MailPurposes.EmailVerification, verificationCode));
        return Task.CompletedTask;
    }

    public Task SendChangeEmailOldMailAsync(string to, string fullName, string newEmail, string verificationCode)
    {
        _sentMails.Enqueue(new SentMail(to, MailPurposes.EmailChangeOld, verificationCode));
        return Task.CompletedTask;
    }

    public Task SendChangeEmailMailAsync(string to, string fullName, string verificationCode)
    {
        _sentMails.Enqueue(new SentMail(to, MailPurposes.EmailChangeNew, verificationCode));
        return Task.CompletedTask;
    }
}

public static class MailPurposes
{
    public const string Generic = "generic";
    public const string EmailVerification = "email-verification";
    public const string PasswordReset = "password-reset";
    public const string EmailChangeOld = "email-change-old";
    public const string EmailChangeNew = "email-change-new";
}
