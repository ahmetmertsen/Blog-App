using buduns_server.Application.Common.Behaviors;
using Microsoft.Extensions.Logging.Abstractions;

namespace buduns_server.UnitTests.Behaviors;

public class LoggingBehaviorTests
{
    [Fact]
    public async Task Handle_SuccessfulRequest_ShouldReturnNextResult()
    {
        var behavior = new LoggingBehavior<TestRequest, string>(NullLogger<LoggingBehavior<TestRequest, string>>.Instance);

        var result = await behavior.Handle(new TestRequest(), _ => Task.FromResult("response"), CancellationToken.None);

        Assert.Equal("response", result);
    }

    [Fact]
    public async Task Handle_FailedRequest_ShouldRethrowOriginalException()
    {
        var behavior = new LoggingBehavior<TestRequest, string>(NullLogger<LoggingBehavior<TestRequest, string>>.Instance);
        var expected = new InvalidOperationException("failure");

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            behavior.Handle(new TestRequest(), _ => Task.FromException<string>(expected), CancellationToken.None));

        Assert.Same(expected, actual);
    }

    private sealed class TestRequest
    {
    }
}
