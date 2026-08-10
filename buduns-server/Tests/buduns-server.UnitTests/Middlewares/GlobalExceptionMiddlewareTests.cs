using System.Text.Json;
using buduns_server.Application.Exceptions;
using buduns_server.WebAPI.Middlewares;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace buduns_server.UnitTests.Middlewares;

public class GlobalExceptionMiddlewareTests
{
    [Fact]
    public async Task Invoke_ValidationException_ShouldReturnStructured400()
    {
        var exception = new ValidationException(new[]
        {
            new ValidationFailure("Email", "Email zorunludur."),
            new ValidationFailure("Email", "Email geçersizdir.")
        });

        var result = await InvokeAsync(exception);

        Assert.Equal(StatusCodes.Status400BadRequest, result.Context.Response.StatusCode);
        Assert.Equal("VALIDATION_ERROR", result.Json.RootElement.GetProperty("error").GetProperty("code").GetString());
        Assert.Equal(2, result.Json.RootElement.GetProperty("error").GetProperty("validationErrors").GetProperty("Email").GetArrayLength());
    }

    [Theory]
    [MemberData(nameof(ApplicationExceptionCases))]
    public async Task Invoke_ApplicationException_ShouldReturnExpectedContract(Exception exception, int statusCode, string errorCode)
    {
        var result = await InvokeAsync(exception);

        Assert.Equal(statusCode, result.Context.Response.StatusCode);
        Assert.Equal("application/json", result.Context.Response.ContentType);
        Assert.False(result.Json.RootElement.GetProperty("isSuccess").GetBoolean());
        Assert.Equal(errorCode, result.Json.RootElement.GetProperty("error").GetProperty("code").GetString());
        Assert.False(string.IsNullOrWhiteSpace(result.Json.RootElement.GetProperty("traceId").GetString()));
    }

    [Fact]
    public async Task Invoke_UnexpectedException_ShouldReturnGeneric500WithoutLeakingMessage()
    {
        var result = await InvokeAsync(new InvalidOperationException("sensitive internal detail"));
        var json = result.Json.RootElement.ToString();

        Assert.Equal(StatusCodes.Status500InternalServerError, result.Context.Response.StatusCode);
        Assert.Equal("INTERNAL_SERVER_ERROR", result.Json.RootElement.GetProperty("error").GetProperty("code").GetString());
        Assert.DoesNotContain("sensitive internal detail", json);
    }

    public static IEnumerable<object[]> ApplicationExceptionCases()
    {
        yield return new object[] { new BadRequestException("bad"), 400, "BAD_REQUEST" };
        yield return new object[] { new UnauthorizedAccesException("unauthorized"), 401, "UNAUTHORIZED_ACCESS" };
        yield return new object[] { new ForbiddenException("forbidden"), 403, "FORBIDDEN" };
        yield return new object[] { new NotFoundException("missing"), 404, "RESOURCE_NOT_FOUND" };
        yield return new object[] { new TooManyRequestsException("slow down"), 429, "TOO_MANY_REQUESTS" };
    }

    private static async Task<(DefaultHttpContext Context, JsonDocument Json)> InvokeAsync(Exception exception)
    {
        var context = new DefaultHttpContext();
        context.TraceIdentifier = "test-trace-id";
        context.Response.Body = new MemoryStream();
        var middleware = new GlobalExceptionMiddleware(
            _ => Task.FromException(exception),
            NullLogger<GlobalExceptionMiddleware>.Instance);

        await middleware.InvokeAsync(context);
        context.Response.Body.Position = 0;
        var json = await JsonDocument.ParseAsync(context.Response.Body);
        return (context, json);
    }
}
