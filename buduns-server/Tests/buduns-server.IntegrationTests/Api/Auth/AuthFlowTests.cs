using buduns_server.Application.Common.Consts;
using buduns_server.Application.Features.Auth.ChangeEmail;
using buduns_server.Application.Features.Auth.Login;
using buduns_server.Application.Features.Auth.Register;
using buduns_server.Application.Features.Users.Commands.Update.UpdateEmail;
using buduns_server.Application.Features.Users.Commands.Update.UpdateMailVerify;
using buduns_server.Application.Features.Users.Commands.Update.UpdatePassword;
using buduns_server.Domain.Enums;
using buduns_server.IntegrationTests.Fixtures;
using buduns_server.IntegrationTests.Helpers;
using buduns_server.Persistence.Context;
using buduns_server.WebAPI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace buduns_server.IntegrationTests.Api.Auth;

/// <summary>
/// Kayit, e-posta dogrulama, giris ve e-posta/sifre degisikligi uctan uca
/// birbirine bagli: her adim bir onceki adimda uretilen dogrulama kodunu
/// tuketiyor. Kodlar veritabaninda HMAC olarak durdugu icin bu akis ancak
/// gercek servislerle dogrulanabilir.
/// </summary>
public sealed class AuthFlowTests : IntegrationTestBase
{
    public AuthFlowTests(BudunsWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Register_then_verify_email_then_login_should_produce_usable_token()
    {
        using var client = Factory.CreateHttpsClient();

        var registerResponse = await client.PostAsJsonAsync("/api/User/register", new RegisterUserCommand("flow-user", "Flow User", "flow-user@integration.test", "Integration123!"));
        var registerBody = await registerResponse.ReadDataAsync<RegisterUserCommandResponse>();

        registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        registerBody.Message.Should().NotBeNullOrWhiteSpace();

        // Kayit "User" rolunu otomatik atamali ve e-posta dogrulanmamis olmali.
        var created = await Factory.ExecuteScopeAsync(async services =>
            await services.GetRequiredService<BudunsDbContext>().Users.AsNoTracking().SingleAsync(user => user.UserName == "flow-user"));
        created.EmailConfirmed.Should().BeFalse();
        created.EmailVerificationSentAt.Should().NotBeNull();

        var loginResponse = await client.PostAsJsonAsync("/api/Auth/login", new LoginUserCommand("flow-user", "Integration123!"));
        var loginBody = await loginResponse.ReadDataAsync<LoginUserCommandResponse>();
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        // Dogrulanmamis hesap giris yapabilir ama token bunu bildirmelidir.
        loginBody!.Token.RequiresEmailVerification.Should().BeTrue();

        using var authenticated = await Factory.CreateAuthenticatedClientAsync(created.Id);
        var verificationCode = Factory.MailService.LastVerificationCodeFor("flow-user@integration.test", MailPurposes.EmailVerification);

        var verifyResponse = await authenticated.Client.PostAsJsonAsync("/api/User/updateMailVerify", new UpdateUserMailVerifyCommand { VerificationCode = verificationCode });
        verifyResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var verified = await Factory.ExecuteScopeAsync(async services =>
            await services.GetRequiredService<BudunsDbContext>().Users.AsNoTracking().SingleAsync(user => user.Id == created.Id));
        verified.EmailConfirmed.Should().BeTrue();
    }

    [Fact]
    public async Task Register_with_duplicate_email_or_username_should_be_rejected()
    {
        await CreateUserAsync("dup-user");
        using var client = Factory.CreateHttpsClient();

        var duplicateEmail = await client.PostAsJsonAsync("/api/User/register", new RegisterUserCommand("baska-ad", "Baska Ad", "dup-user@integration.test", "Integration123!"));
        var duplicateUserName = await client.PostAsJsonAsync("/api/User/register", new RegisterUserCommand("dup-user", "Dup User", "baska@integration.test", "Integration123!"));

        duplicateEmail.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await duplicateEmail.ReadErrorAsync()).Code.Should().Be("REGISTER_FAILED");
        duplicateUserName.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Unverified_user_should_be_blocked_on_endpoints_that_require_verification()
    {
        var user = await CreateUserAsync("unverified-user");
        await Factory.ExecuteScopeAsync(services => DatabaseSeeder.SetEmailConfirmedAsync(services, user.Id, false));
        var post = await Factory.ExecuteScopeAsync(services => DatabaseSeeder.CreatePostAsync(services, user.Id));
        using var authentication = await Factory.CreateAuthenticatedClientAsync(user.Id);

        var blocked = await authentication.Client.PostAsync($"/api/Like/{post.Id}", null);
        // Oturum uclari IAllowUnverifiedEmail tasidigi icin acik kalmali.
        var allowed = await authentication.Client.GetAsync("/api/Auth/sessions");

        blocked.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await blocked.ReadErrorAsync()).Code.Should().Be("EMAIL_VERIFICATION_REQUIRED");
        allowed.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Login_with_wrong_password_or_unknown_user_should_return_unauthorized()
    {
        await CreateUserAsync("login-user");
        using var client = Factory.CreateHttpsClient();

        var wrongPassword = await client.PostAsJsonAsync("/api/Auth/login", new LoginUserCommand("login-user", "YanlisSifre1!"));
        var unknownUser = await client.PostAsJsonAsync("/api/Auth/login", new LoginUserCommand("olmayan-kullanici", "Integration123!"));

        wrongPassword.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        unknownUser.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        // Iki durumda da ayni mesaj donmeli: kullanici varligi sizdirilmamali.
        (await wrongPassword.ReadErrorAsync()).Message
            .Should().Be((await unknownUser.ReadErrorAsync()).Message);
    }

    /// <summary>
    /// Basarisiz giris sayaci istisna firlatilmadan once yaziliyor. Bu davranis
    /// kayit altinda degildi ve transaction siniri gelince fark edildi: giris
    /// komutu transaction'a alinsaydi rollback sayaci da silecek, hesap asla
    /// kilitlenmeyecekti. Test o sozlesmeyi kilitliyor.
    /// </summary>
    [Fact]
    public async Task Failed_login_attempts_should_survive_and_lock_the_account()
    {
        await CreateUserAsync("lockout-user");
        using var client = Factory.CreateHttpsClient();
        var wrongCredentials = new LoginUserCommand("lockout-user", "YanlisSifre1!");

        // Identity yapilandirmasi: MaxFailedAccessAttempts = 5.
        for (var attempt = 0; attempt < 4; attempt++)
        {
            (await client.PostAsJsonAsync("/api/Auth/login", wrongCredentials)).StatusCode
                .Should().Be(HttpStatusCode.Unauthorized);
        }

        var afterFourFailures = await ReadLockoutStateAsync();
        afterFourFailures.AccessFailedCount.Should().Be(4, "sayac istisnaya ragmen kalici olmali");
        afterFourFailures.LockoutEnd.Should().BeNull("esik asilmadan kilitlenmemeli");

        (await client.PostAsJsonAsync("/api/Auth/login", wrongCredentials)).StatusCode
            .Should().Be(HttpStatusCode.Unauthorized);

        // Esige gelince Identity sayaci sifirlayip kilit bitis zamanini yaziyor.
        var afterLimit = await ReadLockoutStateAsync();
        afterLimit.LockoutEnd.Should().NotBeNull("besinci hatali denemede hesap kilitlenmeli");

        // Dogru sifreyle bile girilemez: hesap kilitli.
        var afterLockout = await client.PostAsJsonAsync("/api/Auth/login", new LoginUserCommand("lockout-user", "Integration123!"));

        afterLockout.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await afterLockout.ReadErrorAsync()).Message.Should().Contain("Çok fazla başarısız giriş");

        Task<(int AccessFailedCount, DateTimeOffset? LockoutEnd)> ReadLockoutStateAsync() =>
            Factory.ExecuteScopeAsync(services => services.GetRequiredService<BudunsDbContext>().Users
                .Where(user => user.UserName == "lockout-user")
                .Select(user => new ValueTuple<int, DateTimeOffset?>(user.AccessFailedCount, user.LockoutEnd))
                .SingleAsync());
    }

    [Fact]
    public async Task Login_with_email_should_work_like_login_with_username()
    {
        await CreateUserAsync("email-login-user");
        using var client = Factory.CreateHttpsClient();

        var response = await client.PostAsJsonAsync("/api/Auth/login", new LoginUserCommand("email-login-user@integration.test", DatabaseSeeder.DefaultPassword));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Banned_user_should_not_be_able_to_login()
    {
        var user = await CreateUserAsync("banned-login-user");
        await Factory.ExecuteScopeAsync(services => DatabaseSeeder.SetUserStatusAsync(services, user.Id, UserStatus.Banned));
        using var client = Factory.CreateHttpsClient();

        var response = await client.PostAsJsonAsync("/api/Auth/login", new LoginUserCommand("banned-login-user", DatabaseSeeder.DefaultPassword));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await response.ReadErrorAsync()).Message.Should().Contain("yasaklan");
    }

    [Fact]
    public async Task Suspended_user_should_not_login_until_suspension_expires()
    {
        var user = await CreateUserAsync("suspended-login-user");
        await Factory.ExecuteScopeAsync(services => DatabaseSeeder.SetUserStatusAsync(services, user.Id, UserStatus.Suspended, DateTime.UtcNow.AddHours(1)));
        using var client = Factory.CreateHttpsClient();

        var blocked = await client.PostAsJsonAsync("/api/Auth/login", new LoginUserCommand("suspended-login-user", DatabaseSeeder.DefaultPassword));
        blocked.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // Suresi dolmus askiya alma girise izin vermeli ve hesabi tekrar aktif etmeli.
        await Factory.ExecuteScopeAsync(services => DatabaseSeeder.SetUserStatusAsync(services, user.Id, UserStatus.Suspended, DateTime.UtcNow.AddMinutes(-1)));

        var allowed = await client.PostAsJsonAsync("/api/Auth/login", new LoginUserCommand("suspended-login-user", DatabaseSeeder.DefaultPassword));
        allowed.StatusCode.Should().Be(HttpStatusCode.OK);

        var reactivated = await Factory.ExecuteScopeAsync(async services =>
            await services.GetRequiredService<BudunsDbContext>().Users.AsNoTracking().SingleAsync(item => item.Id == user.Id));
        reactivated.Status.Should().Be(UserStatus.Active);
        reactivated.SuspendedUntil.Should().BeNull();
    }

    [Fact]
    public async Task Password_reset_should_consume_code_change_password_and_revoke_sessions()
    {
        var user = await CreateUserAsync("reset-user");
        using var authentication = await Factory.CreateAuthenticatedClientAsync(user.Id);
        using var client = Factory.CreateHttpsClient();

        // Kod uretimi icin AuthService dogrudan kullaniliyor; /api/Auth/forgotPassword
        // yolunun rate limit sayaci HttpPipelineTests'e ayrilmis durumda.
        var verificationCode = await Factory.ExecuteScopeAsync(async services =>
        {
            await services.GetRequiredService<Application.Abstractions.Services.IAuthService>()
                .ForgotPasswordResetAsync(new Application.Dtos.Auth.ForgotPasswordRequest { EmailOrUsername = "reset-user" }, CancellationToken.None);
            return Factory.MailService.LastVerificationCodeFor("reset-user@integration.test", MailPurposes.PasswordReset);
        });

        var response = await client.PostAsJsonAsync("/api/User/updatePassword", new UpdateUserPasswordCommand
        {
            EmailOrUsername = "reset-user",
            VerificationCode = verificationCode,
            newPassword = "YeniSifre123!",
            newPasswordConfirmed = "YeniSifre123!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Sifre degisince tum oturumlar kapatilmali.
        var activeSessionCount = await Factory.ExecuteScopeAsync(async services =>
            await services.GetRequiredService<BudunsDbContext>().AuthSessions.CountAsync(session => session.UserId == user.Id && session.RevokedAt == null));
        activeSessionCount.Should().Be(0);
        (await authentication.Client.GetAsync("/api/Auth/sessions")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        (await client.PostAsJsonAsync("/api/Auth/login", new LoginUserCommand("reset-user", "YeniSifre123!"))).StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.PostAsJsonAsync("/api/Auth/login", new LoginUserCommand("reset-user", DatabaseSeeder.DefaultPassword))).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Password_reset_code_should_be_single_use()
    {
        var user = await CreateUserAsync("single-use-user");
        using var client = Factory.CreateHttpsClient();
        var verificationCode = await Factory.ExecuteScopeAsync(async services =>
        {
            await services.GetRequiredService<Application.Abstractions.Services.IAuthService>()
                .ForgotPasswordResetAsync(new Application.Dtos.Auth.ForgotPasswordRequest { EmailOrUsername = "single-use-user" }, CancellationToken.None);
            return Factory.MailService.LastVerificationCodeFor("single-use-user@integration.test", MailPurposes.PasswordReset);
        });

        UpdateUserPasswordCommand BuildCommand(string password) => new()
        {
            EmailOrUsername = "single-use-user",
            VerificationCode = verificationCode,
            newPassword = password,
            newPasswordConfirmed = password
        };

        var first = await client.PostAsJsonAsync("/api/User/updatePassword", BuildCommand("YeniSifre123!"));
        var second = await client.PostAsJsonAsync("/api/User/updatePassword", BuildCommand("BaskaSifre123!"));

        first.StatusCode.Should().Be(HttpStatusCode.OK);
        second.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        user.Id.Should().BePositive();
    }

    [Fact]
    public async Task Password_reset_with_wrong_code_should_fail()
    {
        await CreateUserAsync("wrong-code-user");
        using var client = Factory.CreateHttpsClient();
        await Factory.ExecuteScopeAsync(services => services.GetRequiredService<Application.Abstractions.Services.IAuthService>()
            .ForgotPasswordResetAsync(new Application.Dtos.Auth.ForgotPasswordRequest { EmailOrUsername = "wrong-code-user" }, CancellationToken.None));

        var response = await client.PostAsJsonAsync("/api/User/updatePassword", new UpdateUserPasswordCommand
        {
            EmailOrUsername = "wrong-code-user",
            VerificationCode = "000000",
            newPassword = "YeniSifre123!",
            newPasswordConfirmed = "YeniSifre123!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Email_change_should_require_both_codes_and_revoke_sessions()
    {
        var user = await CreateUserAsync("change-email-user");
        using var authentication = await Factory.CreateAuthenticatedClientAsync(user.Id);

        var requestResponse = await authentication.Client.PostAsJsonAsync("/api/Auth/emailChange", new ChangeEmailCommand { NewEmail = "yeni-adres@integration.test" });
        requestResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var oldEmailCode = Factory.MailService.LastVerificationCodeFor("change-email-user@integration.test", MailPurposes.EmailChangeOld);
        var newEmailCode = Factory.MailService.LastVerificationCodeFor("yeni-adres@integration.test", MailPurposes.EmailChangeNew);
        oldEmailCode.Should().NotBe(newEmailCode);

        var wrongCodes = await authentication.Client.PostAsJsonAsync("/api/User/updateUserEmail", new UpdateUserEmailCommand
        {
            OldEmailVerificationCode = "000000",
            NewEmailVerificationCode = newEmailCode,
            NewEmail = "yeni-adres@integration.test"
        });
        wrongCodes.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var response = await authentication.Client.PostAsJsonAsync("/api/User/updateUserEmail", new UpdateUserEmailCommand
        {
            OldEmailVerificationCode = oldEmailCode,
            NewEmailVerificationCode = newEmailCode,
            NewEmail = "yeni-adres@integration.test"
        });
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var updated = await Factory.ExecuteScopeAsync(async services =>
            await services.GetRequiredService<BudunsDbContext>().Users.AsNoTracking().SingleAsync(item => item.Id == user.Id));
        updated.Email.Should().Be("yeni-adres@integration.test");

        var activeSessionCount = await Factory.ExecuteScopeAsync(async services =>
            await services.GetRequiredService<BudunsDbContext>().AuthSessions.CountAsync(session => session.UserId == user.Id && session.RevokedAt == null));
        activeSessionCount.Should().Be(0);
    }

    [Fact]
    public async Task Email_change_to_an_address_already_in_use_should_not_leak_that_information()
    {
        await CreateUserAsync("taken-email-user");
        var user = await CreateUserAsync("changer-user");
        using var authentication = await Factory.CreateAuthenticatedClientAsync(user.Id);

        var response = await authentication.Client.PostAsJsonAsync("/api/Auth/emailChange", new ChangeEmailCommand { NewEmail = "taken-email-user@integration.test" });

        // Cevap notr: adres kayitli olsa da olmasa da ayni. Ama kod uretilmemeli.
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Factory.MailService.SentMails.Should().NotContain(mail => mail.Purpose == MailPurposes.EmailChangeNew);
    }

    [Fact]
    public async Task Mail_verify_request_should_be_throttled_within_one_minute()
    {
        var user = await CreateUserAsync("resend-user");
        await Factory.ExecuteScopeAsync(services => DatabaseSeeder.SetEmailConfirmedAsync(services, user.Id, false));
        using var authentication = await Factory.CreateAuthenticatedClientAsync(user.Id);

        var first = await authentication.Client.PostAsync("/api/Auth/mailVerify", null);
        var second = await authentication.Client.PostAsync("/api/Auth/mailVerify", null);

        first.StatusCode.Should().Be(HttpStatusCode.OK);
        second.StatusCode.Should().Be(HttpStatusCode.OK);
        // Ikinci istek yeni kod uretmemeli.
        Factory.MailService.SentMails.Count(mail => mail.Purpose == MailPurposes.EmailVerification).Should().Be(1);
    }

    [Fact]
    public async Task Mail_verify_for_already_confirmed_user_should_not_send_a_code()
    {
        var user = await CreateUserAsync("already-verified-user");
        using var authentication = await Factory.CreateAuthenticatedClientAsync(user.Id);

        var response = await authentication.Client.PostAsync("/api/Auth/mailVerify", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Factory.MailService.SentMails.Should().BeEmpty();
    }

    [Fact]
    public async Task Forgot_password_for_unknown_user_should_return_the_same_neutral_response()
    {
        // Not: /api/Auth/forgotPassword yoluna HTTP uzerinden gidilmiyor; o yolun
        // rate limit sayaci HttpPipelineTests'e ait. Servis dogrudan cagriliyor.
        var response = await Factory.ExecuteScopeAsync(services => services.GetRequiredService<Application.Abstractions.Services.IAuthService>()
            .ForgotPasswordResetAsync(new Application.Dtos.Auth.ForgotPasswordRequest { EmailOrUsername = "olmayan-kullanici" }, CancellationToken.None));

        response.Message.Should().NotBeNullOrWhiteSpace();
        Factory.MailService.SentMails.Should().BeEmpty();
    }

    [Fact]
    public async Task Verification_code_should_expire_after_max_attempts()
    {
        await CreateUserAsync("attempt-user");
        using var client = Factory.CreateHttpsClient();
        await Factory.ExecuteScopeAsync(services => services.GetRequiredService<Application.Abstractions.Services.IAuthService>()
            .ForgotPasswordResetAsync(new Application.Dtos.Auth.ForgotPasswordRequest { EmailOrUsername = "attempt-user" }, CancellationToken.None));

        UpdateUserPasswordCommand command = new()
        {
            EmailOrUsername = "attempt-user",
            VerificationCode = "000000",
            newPassword = "YeniSifre123!",
            newPasswordConfirmed = "YeniSifre123!"
        };

        // Varsayilan MaxAttempts 5; besinci hatali denemeden sonra kod kilitlenir.
        for (var attempt = 0; attempt < 5; attempt++)
        {
            (await client.PostAsJsonAsync("/api/User/updatePassword", command)).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        var afterLimit = await client.PostAsJsonAsync("/api/User/updatePassword", command);
        afterLimit.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task Role_assignment_should_revoke_all_sessions_of_the_target_user()
    {
        var admin = await CreateUserAsync("session-admin", RoleConstants.Admin);
        var target = await CreateUserAsync("session-target");
        using var adminAuthentication = await Factory.CreateAuthenticatedClientAsync(admin.Id);
        using var targetAuthentication = await Factory.CreateAuthenticatedClientAsync(target.Id);

        (await targetAuthentication.Client.GetAsync("/api/Auth/sessions")).StatusCode.Should().Be(HttpStatusCode.OK);

        var response = await adminAuthentication.Client.PostAsJsonAsync("/api/User/assignRoleToUser", new Application.Features.Users.Commands.AssignRoleToUser.AssignRoleToUserCommand
        {
            TargetUserId = target.Id,
            Roles = new[] { RoleConstants.Moderator }
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await targetAuthentication.Client.GetAsync("/api/Auth/sessions")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
