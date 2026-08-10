using buduns_server.Application.Common.Consts;
using buduns_server.Application.Common.Options;
using buduns_server.Application.Repositories;
using buduns_server.Application.UnitOfWork;
using buduns_server.Domain.Entities;
using buduns_server.Infrastructure.Services.Mail;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace buduns_server.UnitTests.Services;

/// <summary>
/// Gercek SMTP gonderimi test edilmiyor; test edilen, gonderim denenmeden once
/// verilen kararlar: sablon var mi, yapilandirma tam mi. Ikisi de eksikken
/// hata System.Net.Mail'in icinden anlamsiz bir mesajla cikiyordu.
/// </summary>
public class MailServiceTests
{
    [Fact]
    public async Task SendMail_WithoutSmtpConfiguration_ShouldSayWhichSettingsAreMissing()
    {
        var service = CreateService(new MailOptions { Host = "smtp.example.com" });

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.SendMailAsync("kime@example.com", "konu", "icerik"));

        Assert.Contains("Mail:Username", exception.Message);
        Assert.Contains("Mail:Password", exception.Message);
        Assert.DoesNotContain("Mail:Host", exception.Message);
    }

    [Fact]
    public async Task SendVerifyMail_WithoutSmtpConfiguration_ShouldFailOnConfigurationNotOnAddressParsing()
    {
        var service = CreateService(new MailOptions(), MailTemplateKeys.MailVerify);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.SendVerifyMailAsync("kime@example.com", "Ad Soyad", "123456"));

        Assert.Contains("SMTP yapilandirmasi eksik", exception.Message);
    }

    [Fact]
    public async Task SendVerifyMail_WithoutTemplate_ShouldNameTheMissingTemplate()
    {
        var service = CreateService(FullyConfigured());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.SendVerifyMailAsync("kime@example.com", "Ad Soyad", "123456"));

        Assert.Contains(MailTemplateKeys.MailVerify, exception.Message);
    }

    [Theory]
    [InlineData("", "sifre", "host")]
    [InlineData("kullanici", "", "host")]
    [InlineData("kullanici", "sifre", "")]
    [InlineData("   ", "sifre", "host")]
    public void IsConfigured_ShouldRequireEverySmtpSetting(string username, string password, string host)
    {
        var options = new MailOptions { Username = username, Password = password, Host = host };

        Assert.False(options.IsConfigured);
        Assert.NotEmpty(options.GetMissingSettings());
    }

    [Fact]
    public void IsConfigured_ShouldBeTrueWhenEverySmtpSettingIsPresent()
    {
        Assert.True(FullyConfigured().IsConfigured);
        Assert.Empty(FullyConfigured().GetMissingSettings());
    }

    private static MailOptions FullyConfigured() => new()
    {
        Username = "gonderen@example.com",
        Password = "sifre",
        Host = "smtp.example.com",
        Port = 587,
        FromName = "Buduns"
    };

    private static MailService CreateService(MailOptions options, string? availableTemplateKey = null)
    {
        var utilityRepository = Substitute.For<IUtilityRepository>();
        utilityRepository.GetByNameAsync(Arg.Any<string>()).Returns((Utility?)null);

        if (availableTemplateKey != null)
        {
            utilityRepository.GetByNameAsync(availableTemplateKey)
                .Returns(new Utility { Name = availableTemplateKey, Value = "<p>{full_name} {verification_code} {app_name}</p>" });
        }

        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.UtilityRepository.Returns(utilityRepository);

        return new MailService(Options.Create(options), unitOfWork, NullLogger<MailService>.Instance);
    }
}
