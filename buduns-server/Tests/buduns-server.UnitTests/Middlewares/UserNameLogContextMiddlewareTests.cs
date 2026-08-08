using System.Security.Claims;
using buduns_server.WebAPI.Middlewares;
using Microsoft.AspNetCore.Http;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace buduns_server.UnitTests.Middlewares;

/// <summary>
/// Serilog'un Postgres sink'i "user_name" sutununu LogContext'ten okuyor.
/// Ozellik itilmezse log tablosunda kullanici sutunu bos kalir.
/// </summary>
public class UserNameLogContextMiddlewareTests
{
    [Fact]
    public async Task Invoke_AuthenticatedUser_ShouldPushUserNameToLogContext()
    {
        var sink = new CollectingSink();
        using var logger = new LoggerConfiguration().Enrich.FromLogContext().WriteTo.Sink(sink).CreateLogger();
        var previousLogger = Log.Logger;
        Log.Logger = logger;

        try
        {
            var middleware = new UserNameLogContextMiddleware(_ =>
            {
                Log.Information("istek islendi");
                return Task.CompletedTask;
            });

            await middleware.InvokeAsync(CreateContext("ahmet", authenticated: true));
        }
        finally
        {
            Log.Logger = previousLogger;
        }

        var logEvent = Assert.Single(sink.Events);
        Assert.True(logEvent.Properties.TryGetValue("user_name", out var userName));
        Assert.Equal("\"ahmet\"", userName!.ToString());
    }

    [Fact]
    public async Task Invoke_AnonymousUser_ShouldNotPushUserName()
    {
        var sink = new CollectingSink();
        using var logger = new LoggerConfiguration().Enrich.FromLogContext().WriteTo.Sink(sink).CreateLogger();
        var previousLogger = Log.Logger;
        Log.Logger = logger;

        try
        {
            var middleware = new UserNameLogContextMiddleware(_ =>
            {
                Log.Information("anonim istek");
                return Task.CompletedTask;
            });

            await middleware.InvokeAsync(new DefaultHttpContext());
        }
        finally
        {
            Log.Logger = previousLogger;
        }

        var logEvent = Assert.Single(sink.Events);
        Assert.False(logEvent.Properties.ContainsKey("user_name"));
    }

    [Fact]
    public async Task Invoke_AuthenticatedWithoutName_ShouldStillCallNext()
    {
        var nextCalled = false;
        var middleware = new UserNameLogContextMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(CreateContext(userName: null, authenticated: true));

        Assert.True(nextCalled);
    }

    [Fact]
    public async Task Invoke_ShouldPropagateExceptionFromNext()
    {
        var middleware = new UserNameLogContextMiddleware(_ => Task.FromException(new InvalidOperationException("hata")));

        await Assert.ThrowsAsync<InvalidOperationException>(() => middleware.InvokeAsync(CreateContext("ahmet", authenticated: true)));
    }

    private static DefaultHttpContext CreateContext(string? userName, bool authenticated)
    {
        var claims = userName == null ? Array.Empty<Claim>() : new[] { new Claim(ClaimTypes.Name, userName) };
        var identity = new ClaimsIdentity(claims, authenticated ? "TestAuthentication" : null);
        return new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
    }

    private sealed class CollectingSink : ILogEventSink
    {
        public List<LogEvent> Events { get; } = new();

        public void Emit(LogEvent logEvent) => Events.Add(logEvent);
    }
}
