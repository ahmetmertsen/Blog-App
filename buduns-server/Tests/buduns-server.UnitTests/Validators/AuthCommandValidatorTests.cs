using buduns_server.Application.Features.Auth.ChangeEmail;
using buduns_server.Application.Features.Auth.ForgotPassword;
using buduns_server.Application.Features.Auth.Login;
using buduns_server.Application.Features.Auth.Register;
using buduns_server.Application.Features.Auth.RevokeSession;

namespace buduns_server.UnitTests.Validators;

/// <summary>
/// AuthValidatorTests mutlu yolu ve birkac hatayi kapsiyordu; burada kalan
/// auth komutlari ile uzunluk sinirlarinin tam degerleri dogrulaniyor.
/// </summary>
public class AuthCommandValidatorTests
{
    [Fact]
    public async Task Login_CredentialAtMaxLength_ShouldSucceed()
    {
        var result = await new LoginUserCommandValidator().ValidateAsync(new LoginUserCommand(new string('a', 100), "secret"));

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Login_CredentialLongerThanLimit_ShouldFail()
    {
        var result = await new LoginUserCommandValidator().ValidateAsync(new LoginUserCommand(new string('a', 101), "secret"));

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(LoginUserCommand.UsernameOrEmail));
    }

    [Fact]
    public async Task Login_PasswordShorterThanLimit_ShouldFail()
    {
        var result = await new LoginUserCommandValidator().ValidateAsync(new LoginUserCommand("ahmet", "12345"));

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(LoginUserCommand.Password));
    }

    [Fact]
    public async Task Register_FieldsAtBoundaryLengths_ShouldSucceed()
    {
        var result = await new RegisterUserCommandValidator().ValidateAsync(new RegisterUserCommand(
            new string('u', 50),
            new string('f', 100),
            "ahmet@example.com",
            "123456"));

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Register_MinimumUsernameLength_ShouldSucceed()
    {
        var result = await new RegisterUserCommandValidator().ValidateAsync(new RegisterUserCommand("abc", "Ahmet", "ahmet@example.com", "123456"));

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(51, 100, nameof(RegisterUserCommand.UserName))]
    [InlineData(50, 101, nameof(RegisterUserCommand.FullName))]
    public async Task Register_FieldLongerThanLimit_ShouldFail(int userNameLength, int fullNameLength, string propertyName)
    {
        var result = await new RegisterUserCommandValidator().ValidateAsync(new RegisterUserCommand(
            new string('u', userNameLength),
            new string('f', fullNameLength),
            "ahmet@example.com",
            "123456"));

        Assert.Contains(result.Errors, error => error.PropertyName == propertyName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Register_BlankUsername_ShouldFail(string userName)
    {
        var result = await new RegisterUserCommandValidator().ValidateAsync(new RegisterUserCommand(userName, "Ahmet", "ahmet@example.com", "123456"));

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(RegisterUserCommand.UserName));
    }

    [Fact]
    public async Task ForgotPassword_ValidRequest_ShouldSucceed()
    {
        var result = await new ForgotPasswordCommandValidator().ValidateAsync(new ForgotPasswordCommand { EmailOrUsername = "ahmet@example.com" });

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ForgotPassword_BlankIdentifier_ShouldFail(string emailOrUsername)
    {
        var result = await new ForgotPasswordCommandValidator().ValidateAsync(new ForgotPasswordCommand { EmailOrUsername = emailOrUsername });

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(ForgotPasswordCommand.EmailOrUsername));
    }

    [Fact]
    public async Task ForgotPassword_IdentifierLongerThanLimit_ShouldFail()
    {
        var result = await new ForgotPasswordCommandValidator().ValidateAsync(new ForgotPasswordCommand { EmailOrUsername = new string('a', 101) });

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(ForgotPasswordCommand.EmailOrUsername));
    }

    [Fact]
    public async Task ChangeEmail_ValidEmail_ShouldSucceed()
    {
        var result = await new ChangeEmailCommandValidator().ValidateAsync(new ChangeEmailCommand { NewEmail = "yeni@example.com" });

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("gecersiz-email")]
    [InlineData("ahmet@")]
    [InlineData("@example.com")]
    public async Task ChangeEmail_InvalidEmail_ShouldFail(string newEmail)
    {
        var result = await new ChangeEmailCommandValidator().ValidateAsync(new ChangeEmailCommand { NewEmail = newEmail });

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(ChangeEmailCommand.NewEmail));
    }

    [Fact]
    public async Task RevokeSession_ValidSessionId_ShouldSucceed()
    {
        var result = await new RevokeSessionCommandValidator().ValidateAsync(new RevokeSessionCommand { SessionId = Guid.NewGuid() });

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task RevokeSession_EmptySessionId_ShouldFail()
    {
        var result = await new RevokeSessionCommandValidator().ValidateAsync(new RevokeSessionCommand { SessionId = Guid.Empty });

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(RevokeSessionCommand.SessionId));
    }
}
