using System.Text.Json;
using buduns_server.WebAPI.Configurations.RateLimiting;
using buduns_server.WebAPI.Middlewares;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace buduns_server.UnitTests.Middlewares;

/// <summary>
/// Sayaclar middleware icinde statik bir sozlukte tutuluyor; bu yuzden her
/// test kendi yoluna ve kendi istemci IP'sine yaziyor. Ayni yolu paylasan iki
/// test birbirinin limitini tuketirdi.
/// </summary>
public class SensitiveEndpointRateLimitMiddlewareTests
{
    [Fact]
    public async Task Invoke_PathWithoutPolicy_ShouldAlwaysCallNext()
    {
        var callCount = 0;
        var middleware = CreateMiddleware("/rate-limit-tests/policy-path", permitLimit: 1, windowSeconds: 60, _ => { callCount++; return Task.CompletedTask; });

        for (var attempt = 0; attempt < 5; attempt++)
        {
            var context = CreateContext("/rate-limit-tests/unprotected-path", "10.0.0.1");
            await middleware.InvokeAsync(context);
            Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        }

        Assert.Equal(5, callCount);
    }

    [Fact]
    public async Task Invoke_WithinLimit_ShouldCallNext()
    {
        const string path = "/rate-limit-tests/within-limit";
        var callCount = 0;
        var middleware = CreateMiddleware(path, permitLimit: 3, windowSeconds: 60, _ => { callCount++; return Task.CompletedTask; });

        for (var attempt = 0; attempt < 3; attempt++)
        {
            await middleware.InvokeAsync(CreateContext(path, "10.0.0.2"));
        }

        Assert.Equal(3, callCount);
    }

    [Fact]
    public async Task Invoke_OverLimit_ShouldReturn429WithRetryAfterAndErrorContract()
    {
        const string path = "/rate-limit-tests/over-limit";
        var callCount = 0;
        var middleware = CreateMiddleware(path, permitLimit: 2, windowSeconds: 60, _ => { callCount++; return Task.CompletedTask; });

        await middleware.InvokeAsync(CreateContext(path, "10.0.0.3"));
        await middleware.InvokeAsync(CreateContext(path, "10.0.0.3"));
        var limited = CreateContext(path, "10.0.0.3");
        await middleware.InvokeAsync(limited);

        Assert.Equal(2, callCount);
        Assert.Equal(StatusCodes.Status429TooManyRequests, limited.Response.StatusCode);
        Assert.Equal("application/json", limited.Response.ContentType);
        Assert.True(int.TryParse(limited.Response.Headers.RetryAfter.ToString(), out var retryAfter) && retryAfter >= 1);

        var body = await ReadBodyAsync(limited);
        Assert.False(body.RootElement.GetProperty("isSuccess").GetBoolean());
        Assert.Equal("RATE_LIMIT_EXCEEDED", body.RootElement.GetProperty("error").GetProperty("code").GetString());
        Assert.Equal(429, body.RootElement.GetProperty("error").GetProperty("httpStatus").GetInt32());
        Assert.False(string.IsNullOrWhiteSpace(body.RootElement.GetProperty("error").GetProperty("traceId").GetString()));
    }

    [Fact]
    public async Task Invoke_DifferentClientIps_ShouldGetSeparateCounters()
    {
        const string path = "/rate-limit-tests/per-ip";
        var callCount = 0;
        var middleware = CreateMiddleware(path, permitLimit: 1, windowSeconds: 60, _ => { callCount++; return Task.CompletedTask; });

        await middleware.InvokeAsync(CreateContext(path, "10.0.0.4"));
        await middleware.InvokeAsync(CreateContext(path, "10.0.0.5"));
        var limited = CreateContext(path, "10.0.0.4");
        await middleware.InvokeAsync(limited);

        Assert.Equal(2, callCount);
        Assert.Equal(StatusCodes.Status429TooManyRequests, limited.Response.StatusCode);
    }

    [Fact]
    public async Task Invoke_PathMatchingShouldBeCaseInsensitive()
    {
        const string path = "/rate-limit-tests/CaseSensitivity";
        var callCount = 0;
        var middleware = CreateMiddleware(path, permitLimit: 1, windowSeconds: 60, _ => { callCount++; return Task.CompletedTask; });

        await middleware.InvokeAsync(CreateContext(path.ToLowerInvariant(), "10.0.0.6"));
        var limited = CreateContext(path.ToUpperInvariant(), "10.0.0.6");
        await middleware.InvokeAsync(limited);

        Assert.Equal(1, callCount);
        Assert.Equal(StatusCodes.Status429TooManyRequests, limited.Response.StatusCode);
    }

    [Fact]
    public async Task Invoke_ExpiredWindow_ShouldResetCounter()
    {
        // Pencere suresi 1 saniye; dolduktan sonra sayac sifirlanmali.
        const string path = "/rate-limit-tests/window-reset";
        var callCount = 0;
        var middleware = CreateMiddleware(path, permitLimit: 1, windowSeconds: 1, _ => { callCount++; return Task.CompletedTask; });

        await middleware.InvokeAsync(CreateContext(path, "10.0.0.7"));
        var limited = CreateContext(path, "10.0.0.7");
        await middleware.InvokeAsync(limited);
        Assert.Equal(StatusCodes.Status429TooManyRequests, limited.Response.StatusCode);

        await Task.Delay(TimeSpan.FromSeconds(1.2));
        var afterWindow = CreateContext(path, "10.0.0.7");
        await middleware.InvokeAsync(afterWindow);

        Assert.Equal(2, callCount);
        Assert.Equal(StatusCodes.Status200OK, afterWindow.Response.StatusCode);
    }

    [Theory]
    [InlineData(0, 60)]
    [InlineData(5, 0)]
    [InlineData(-1, -1)]
    public async Task Invoke_DisabledPolicy_ShouldNotLimit(int permitLimit, int windowSeconds)
    {
        var path = $"/rate-limit-tests/disabled-{permitLimit}-{windowSeconds}";
        var callCount = 0;
        var middleware = CreateMiddleware(path, permitLimit, windowSeconds, _ => { callCount++; return Task.CompletedTask; });

        for (var attempt = 0; attempt < 4; attempt++)
        {
            await middleware.InvokeAsync(CreateContext(path, "10.0.0.8"));
        }

        Assert.Equal(4, callCount);
    }

    [Fact]
    public async Task Invoke_UnknownClientIp_ShouldStillCount()
    {
        const string path = "/rate-limit-tests/unknown-ip";
        var middleware = CreateMiddleware(path, permitLimit: 1, windowSeconds: 60, _ => Task.CompletedTask);

        var first = new DefaultHttpContext();
        first.Request.Path = path;
        first.Response.Body = new MemoryStream();
        await middleware.InvokeAsync(first);

        var second = new DefaultHttpContext();
        second.Request.Path = path;
        second.Response.Body = new MemoryStream();
        await middleware.InvokeAsync(second);

        Assert.Equal(StatusCodes.Status200OK, first.Response.StatusCode);
        Assert.Equal(StatusCodes.Status429TooManyRequests, second.Response.StatusCode);
    }

    private static SensitiveEndpointRateLimitMiddleware CreateMiddleware(string path, int permitLimit, int windowSeconds, RequestDelegate next)
    {
        var options = new SensitiveEndpointRateLimitOptions
        {
            Policies = new Dictionary<string, FixedWindowRateLimitPolicy>
            {
                [path] = new() { PermitLimit = permitLimit, WindowSeconds = windowSeconds }
            }
        };

        return new SensitiveEndpointRateLimitMiddleware(next, Options.Create(options), NullLogger<SensitiveEndpointRateLimitMiddleware>.Instance);
    }

    private static DefaultHttpContext CreateContext(string path, string clientIp)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse(clientIp);
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static async Task<JsonDocument> ReadBodyAsync(HttpContext context)
    {
        context.Response.Body.Position = 0;
        return await JsonDocument.ParseAsync(context.Response.Body);
    }
}
