using buduns_server.Application.Features.AuthorizationEndpoint.Commands.AssignRoleEndpoint;
using buduns_server.Application.Features.AuthorizationEndpoint.Queries.GetRolesToEndpoint;
using buduns_server.Application.Features.Roles.Queries.GetAllByUsername;
using buduns_server.Application.Features.Tags.Commands.Create;
using buduns_server.Application.Features.Tags.Commands.Update;
using buduns_server.Application.Features.Users.Commands.Update.UpdateEmail;
using buduns_server.Application.Features.Users.Commands.Update.UpdateMailVerify;
using buduns_server.Application.Features.Users.Commands.Update.UpdatePassword;
using buduns_server.Application.Features.Users.Commands.Update.UpdateProfile;
using buduns_server.Application.Features.Users.Queries.GetByUsername;
using buduns_server.Application.Features.Users.Queries.GetRolesToUser;

namespace buduns_server.UnitTests.Validators;

/// <summary>
/// Profil, tag ve endpoint yetkilendirme komutlarinin dogrulama kurallari
/// hicbir test tarafindan korunmuyordu.
/// </summary>
public class ProfileTagAndPermissionValidatorTests
{
    [Fact]
    public async Task UpdateProfile_ValidRequest_ShouldSucceed()
    {
        var result = await new UpdateUserProfileCommandValidator().ValidateAsync(new UpdateUserProfileCommand
        {
            FullName = "Ahmet Mert",
            Bio = "Kisa bir tanitim",
            ImageUrl = "https://cdn.example.com/avatar.png"
        });

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task UpdateProfile_NullOptionalFields_ShouldSucceed()
    {
        var result = await new UpdateUserProfileCommandValidator().ValidateAsync(new UpdateUserProfileCommand
        {
            FullName = "Ahmet Mert",
            Bio = null,
            ImageUrl = null
        });

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task UpdateProfile_BlankFullName_ShouldFail(string fullName)
    {
        var result = await new UpdateUserProfileCommandValidator().ValidateAsync(new UpdateUserProfileCommand { FullName = fullName });

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(UpdateUserProfileCommand.FullName));
    }

    [Fact]
    public async Task UpdateProfile_FieldsAtLimit_ShouldSucceed()
    {
        var result = await new UpdateUserProfileCommandValidator().ValidateAsync(new UpdateUserProfileCommand
        {
            FullName = new string('f', 100),
            Bio = new string('b', 1000),
            ImageUrl = new string('i', 500)
        });

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task UpdateProfile_FieldsOverLimit_ShouldFailEachProperty()
    {
        var result = await new UpdateUserProfileCommandValidator().ValidateAsync(new UpdateUserProfileCommand
        {
            FullName = new string('f', 101),
            Bio = new string('b', 1001),
            ImageUrl = new string('i', 501)
        });

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(UpdateUserProfileCommand.FullName));
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(UpdateUserProfileCommand.Bio));
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(UpdateUserProfileCommand.ImageUrl));
    }

    [Fact]
    public async Task UpdatePassword_MismatchedConfirmation_ShouldFail()
    {
        var result = await new UpdateUserPasswordCommandValidator().ValidateAsync(new UpdateUserPasswordCommand
        {
            EmailOrUsername = "ahmet@example.com",
            VerificationCode = "123456",
            newPassword = "123456",
            newPasswordConfirmed = "654321"
        });

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(UpdateUserPasswordCommand.newPasswordConfirmed));
    }

    [Fact]
    public async Task UpdatePassword_ShortPassword_ShouldFailBothPasswordProperties()
    {
        var result = await new UpdateUserPasswordCommandValidator().ValidateAsync(new UpdateUserPasswordCommand
        {
            EmailOrUsername = "ahmet@example.com",
            VerificationCode = "123456",
            newPassword = "12345",
            newPasswordConfirmed = "12345"
        });

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(UpdateUserPasswordCommand.newPassword));
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(UpdateUserPasswordCommand.newPasswordConfirmed));
    }

    [Fact]
    public async Task UpdatePassword_IdentifierLongerThanLimit_ShouldFail()
    {
        var result = await new UpdateUserPasswordCommandValidator().ValidateAsync(new UpdateUserPasswordCommand
        {
            EmailOrUsername = new string('a', 101),
            VerificationCode = "123456",
            newPassword = "123456",
            newPasswordConfirmed = "123456"
        });

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(UpdateUserPasswordCommand.EmailOrUsername));
    }

    [Theory]
    [InlineData("")]
    [InlineData("12345")]
    [InlineData("1234567")]
    [InlineData("12345a")]
    [InlineData("      ")]
    public async Task UpdateMailVerify_InvalidCode_ShouldFail(string verificationCode)
    {
        var result = await new UpdateUserMailVerifyCommandValidator().ValidateAsync(new UpdateUserMailVerifyCommand { VerificationCode = verificationCode });

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(UpdateUserMailVerifyCommand.VerificationCode));
    }

    [Theory]
    [InlineData("12345", "222222", nameof(UpdateUserEmailCommand.OldEmailVerificationCode))]
    [InlineData("111111", "abcdef", nameof(UpdateUserEmailCommand.NewEmailVerificationCode))]
    public async Task UpdateEmail_InvalidCodes_ShouldFail(string oldCode, string newCode, string propertyName)
    {
        var result = await new UpdateUserEmailCommandValidator().ValidateAsync(new UpdateUserEmailCommand
        {
            OldEmailVerificationCode = oldCode,
            NewEmailVerificationCode = newCode,
            NewEmail = "yeni@example.com"
        });

        Assert.Contains(result.Errors, error => error.PropertyName == propertyName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("gecersiz")]
    public async Task UpdateEmail_InvalidNewEmail_ShouldFail(string newEmail)
    {
        var result = await new UpdateUserEmailCommandValidator().ValidateAsync(new UpdateUserEmailCommand
        {
            OldEmailVerificationCode = "111111",
            NewEmailVerificationCode = "222222",
            NewEmail = newEmail
        });

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(UpdateUserEmailCommand.NewEmail));
    }

    [Fact]
    public async Task GetUserByUsername_ValidUserName_ShouldSucceed()
    {
        var result = await new GetUserByUsernameQueryValidator().ValidateAsync(new GetUserByUsernameQuery { UserName = "ahmet" });

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("ab")]
    public async Task GetUserByUsername_TooShortUserName_ShouldFail(string userName)
    {
        var result = await new GetUserByUsernameQueryValidator().ValidateAsync(new GetUserByUsernameQuery { UserName = userName });

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(GetUserByUsernameQuery.UserName));
    }

    [Fact]
    public async Task GetUserByUsername_TooLongUserName_ShouldFail()
    {
        var result = await new GetUserByUsernameQueryValidator().ValidateAsync(new GetUserByUsernameQuery { UserName = new string('a', 51) });

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(GetUserByUsernameQuery.UserName));
    }

    [Fact]
    public async Task GetRolesByUsername_LengthRules_ShouldMatchUserNameLimits()
    {
        var valid = await new GetRolesByUsernameQueryValidator().ValidateAsync(new GetRolesByUsernameQuery { UserName = "ahmet" });
        var tooShort = await new GetRolesByUsernameQueryValidator().ValidateAsync(new GetRolesByUsernameQuery { UserName = "ab" });
        var tooLong = await new GetRolesByUsernameQueryValidator().ValidateAsync(new GetRolesByUsernameQuery { UserName = new string('a', 51) });

        Assert.True(valid.IsValid);
        Assert.Contains(tooShort.Errors, error => error.PropertyName == nameof(GetRolesByUsernameQuery.UserName));
        Assert.Contains(tooLong.Errors, error => error.PropertyName == nameof(GetRolesByUsernameQuery.UserName));
    }

    [Fact]
    public async Task GetRolesToUser_ZeroUserId_ShouldFail()
    {
        var result = await new GetRolesToUserQueryValidator().ValidateAsync(new GetRolesToUserQuery { UserId = 0 });

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(GetRolesToUserQuery.UserId));
    }

    [Fact]
    public async Task GetRolesToUser_PositiveUserId_ShouldSucceed()
    {
        var result = await new GetRolesToUserQueryValidator().ValidateAsync(new GetRolesToUserQuery { UserId = 3 });

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task CreateTag_ValidName_ShouldSucceed()
    {
        var result = await new CreateTagsCommandValidator().ValidateAsync(new CreateTagsCommand("dotnet"));

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateTag_BlankName_ShouldFail(string name)
    {
        var result = await new CreateTagsCommandValidator().ValidateAsync(new CreateTagsCommand(name));

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateTagsCommand.Name));
    }

    [Fact]
    public async Task CreateTag_NameLengthBoundary_ShouldMatchLimit()
    {
        var atLimit = await new CreateTagsCommandValidator().ValidateAsync(new CreateTagsCommand(new string('t', 100)));
        var overLimit = await new CreateTagsCommandValidator().ValidateAsync(new CreateTagsCommand(new string('t', 101)));

        Assert.True(atLimit.IsValid);
        Assert.Contains(overLimit.Errors, error => error.PropertyName == nameof(CreateTagsCommand.Name));
    }

    [Fact]
    public async Task UpdateTag_ValidRequest_ShouldSucceed()
    {
        var result = await new UpdateTagsCommandValidator().ValidateAsync(new UpdateTagsCommand(3, "dotnet"));

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task UpdateTag_InvalidIdAndName_ShouldFailBothProperties()
    {
        var result = await new UpdateTagsCommandValidator().ValidateAsync(new UpdateTagsCommand(0, "  "));

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(UpdateTagsCommand.Id));
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(UpdateTagsCommand.Name));
    }

    [Fact]
    public async Task AssignRoleEndpoint_ValidRequest_ShouldSucceed()
    {
        var result = await new AssignRoleEndpointCommandValidator().ValidateAsync(new AssignRoleEndpointCommand
        {
            Roles = new[] { "Moderator" },
            Code = "POST.Writing.CreatePost",
            Menu = "Posts"
        });

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task AssignRoleEndpoint_MissingFields_ShouldFailEachProperty()
    {
        var result = await new AssignRoleEndpointCommandValidator().ValidateAsync(new AssignRoleEndpointCommand
        {
            Roles = Array.Empty<string>(),
            Code = string.Empty,
            Menu = string.Empty
        });

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(AssignRoleEndpointCommand.Roles));
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(AssignRoleEndpointCommand.Code));
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(AssignRoleEndpointCommand.Menu));
    }

    [Fact]
    public async Task GetRolesToEndpoint_ValidRequest_ShouldSucceed()
    {
        var result = await new GetRolesToEndpointQueryValidator().ValidateAsync(new GetRolesToEndpointQuery
        {
            Code = "POST.Writing.CreatePost",
            Menu = "Posts"
        });

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task GetRolesToEndpoint_MissingFields_ShouldFailEachProperty()
    {
        var result = await new GetRolesToEndpointQueryValidator().ValidateAsync(new GetRolesToEndpointQuery());

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(GetRolesToEndpointQuery.Code));
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(GetRolesToEndpointQuery.Menu));
    }
}
