using buduns_server.Application.Exceptions;
using ApplicationException = buduns_server.Application.Exceptions.ApplicationException;

namespace buduns_server.UnitTests.Exceptions;

/// <summary>
/// GlobalExceptionMiddleware HTTP durum kodunu ve hata kodunu dogrudan
/// exception'dan okuyor. Bir exception'in kodu degisirse istemci sozlesmesi
/// sessizce degisir; bu testler haritayi kilitler.
/// </summary>
public class ApplicationExceptionContractTests
{
    public static IEnumerable<object[]> Exceptions()
    {
        yield return new object[] { new BadRequestException("m"), 400, "BAD_REQUEST" };
        yield return new object[] { new ChangeEmailFailedException("m"), 400, "EMAIL_CHANGE_FAILED" };
        yield return new object[] { new MailVerifyFailedException("m"), 400, "MAIL_VERIFY_FAILED" };
        yield return new object[] { new PasswordChangeFailedException("m"), 400, "PASSWORD_CHANGE_FAILED" };
        yield return new object[] { new RegisterFailedException("m"), 400, "REGISTER_FAILED" };
        yield return new object[] { new UnauthorizedAccesException("m"), 401, "UNAUTHORIZED_ACCESS" };
        yield return new object[] { new InvalidRefreshTokenException("m"), 401, "INVALID_REFRESH_TOKEN" };
        yield return new object[] { new ForbiddenException("m"), 403, "FORBIDDEN" };
        yield return new object[] { new EmailVerificationRequiredException("m"), 403, "EMAIL_VERIFICATION_REQUIRED" };
        yield return new object[] { new NotFoundException("m"), 404, "RESOURCE_NOT_FOUND" };
        yield return new object[] { new ConcurrencyConflictException(), 409, "CONCURRENCY_CONFLICT" };
        yield return new object[] { new TooManyRequestsException("m"), 429, "TOO_MANY_REQUESTS" };
    }

    [Theory]
    [MemberData(nameof(Exceptions))]
    public void Exception_ShouldExposeExpectedStatusAndErrorCode(ApplicationException exception, int httpStatusCode, string errorCode)
    {
        Assert.Equal(httpStatusCode, exception.HttpStatusCode);
        Assert.Equal(errorCode, exception.ErrorCode);
    }

    [Fact]
    public void Exception_ShouldPreserveMessage()
    {
        Assert.Equal("kullanici bulunamadi", new NotFoundException("kullanici bulunamadi").Message);
    }

    [Fact]
    public void ConcurrencyConflict_ShouldHaveDefaultMessage()
    {
        Assert.False(string.IsNullOrWhiteSpace(new ConcurrencyConflictException().Message));
    }

    [Fact]
    public void AllApplicationExceptions_ShouldBeCoveredByContractTable()
    {
        // Yeni bir exception eklendiginde bu tablo guncellenmezse test duser.
        var declaredTypes = typeof(ApplicationException).Assembly
            .GetTypes()
            .Where(type => type.IsClass && !type.IsAbstract && typeof(ApplicationException).IsAssignableFrom(type))
            .Select(type => type.Name)
            .OrderBy(name => name)
            .ToArray();

        var coveredTypes = Exceptions()
            .Select(row => row[0].GetType().Name)
            .OrderBy(name => name)
            .ToArray();

        Assert.Equal(declaredTypes, coveredTypes);
    }
}
