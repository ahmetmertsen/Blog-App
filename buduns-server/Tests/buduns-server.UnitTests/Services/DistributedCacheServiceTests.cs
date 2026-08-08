using buduns_server.Application.Abstractions.Services;
using buduns_server.Infrastructure.Services.Caching;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace buduns_server.UnitTests.Services;

public class DistributedCacheServiceTests
{
    private static readonly TimeSpan OneMinute = TimeSpan.FromMinutes(1);

    // Tek ucus kilitleri statik bir sozlukte tutuluyor; testler birbirinin
    // kilidini beklemesin diye her test kendi anahtarini kullanir.
    private static ICacheService CreateService() =>
        new DistributedCacheService(
            new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions())),
            NullLogger<DistributedCacheService>.Instance);

    [Fact]
    public async Task GetOrSetAsync_SecondCall_ShouldNotInvokeFactoryAgain()
    {
        var service = CreateService();
        var factoryCallCount = 0;

        Task<Payload> Factory(CancellationToken _)
        {
            Interlocked.Increment(ref factoryCallCount);
            return Task.FromResult(new Payload { Value = "first" });
        }

        var first = await service.GetOrSetAsync("test:hit:1", OneMinute, Factory, CancellationToken.None);
        var second = await service.GetOrSetAsync("test:hit:1", OneMinute, Factory, CancellationToken.None);

        Assert.Equal(1, factoryCallCount);
        Assert.Equal("first", first.Value);
        Assert.Equal("first", second.Value);
    }

    [Fact]
    public async Task GetOrSetAsync_ConcurrentCallers_ShouldInvokeFactoryOnce()
    {
        var service = CreateService();
        var factoryCallCount = 0;
        var factoryEntered = new TaskCompletionSource();
        var releaseFactory = new TaskCompletionSource();

        async Task<Payload> Factory(CancellationToken _)
        {
            Interlocked.Increment(ref factoryCallCount);
            factoryEntered.TrySetResult();
            await releaseFactory.Task;
            return new Payload { Value = "single-flight" };
        }

        var firstCaller = Task.Run(() => service.GetOrSetAsync("test:stampede:1", OneMinute, Factory, CancellationToken.None));
        await factoryEntered.Task;

        // Fabrika calisirken gelen istekler ya kilitte bekler ya da kilit
        // birakildiktan sonra onbellekten okur; iki durumda da fabrika
        // ikinci kez calismamali.
        var lateCallers = Enumerable.Range(0, 8)
            .Select(_ => Task.Run(() => service.GetOrSetAsync("test:stampede:1", OneMinute, Factory, CancellationToken.None)))
            .ToArray();

        releaseFactory.SetResult();
        var results = await Task.WhenAll(lateCallers.Prepend(firstCaller));

        Assert.Equal(1, factoryCallCount);
        Assert.All(results, result => Assert.Equal("single-flight", result.Value));
    }

    [Fact]
    public async Task GetOrSetAsync_WhenCacheThrows_ShouldFallBackToFactory()
    {
        var service = new DistributedCacheService(new FailingDistributedCache(), NullLogger<DistributedCacheService>.Instance);
        var factoryCallCount = 0;

        Task<Payload> Factory(CancellationToken _)
        {
            Interlocked.Increment(ref factoryCallCount);
            return Task.FromResult(new Payload { Value = "from-source" });
        }

        var first = await service.GetOrSetAsync("test:down:1", OneMinute, Factory, CancellationToken.None);
        var second = await service.GetOrSetAsync("test:down:1", OneMinute, Factory, CancellationToken.None);

        // Redis erisilemezken istek hata vermez, her seferinde kaynaga duser.
        Assert.Equal("from-source", first.Value);
        Assert.Equal("from-source", second.Value);
        Assert.Equal(2, factoryCallCount);
    }

    [Fact]
    public async Task RemoveAsync_WhenCacheThrows_ShouldNotThrow()
    {
        var service = new DistributedCacheService(new FailingDistributedCache(), NullLogger<DistributedCacheService>.Instance);

        await service.RemoveAsync("test:down:2", CancellationToken.None);
    }

    [Fact]
    public async Task RemoveAsync_ShouldForceFactoryToRunAgain()
    {
        var service = CreateService();
        var factoryCallCount = 0;

        Task<Payload> Factory(CancellationToken _)
        {
            Interlocked.Increment(ref factoryCallCount);
            return Task.FromResult(new Payload { Value = "value" });
        }

        await service.GetOrSetAsync("test:remove:1", OneMinute, Factory, CancellationToken.None);
        await service.RemoveAsync("test:remove:1", CancellationToken.None);
        await service.GetOrSetAsync("test:remove:1", OneMinute, Factory, CancellationToken.None);

        Assert.Equal(2, factoryCallCount);
    }

    [Fact]
    public async Task GetOrSetAsync_WhenCancelled_ShouldThrowOperationCanceled()
    {
        var service = CreateService();
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            service.GetOrSetAsync("test:cancel:1", OneMinute, _ => Task.FromResult(new Payload()), cancellation.Token));
    }

    [Fact]
    public async Task NullCacheService_ShouldInvokeFactoryEveryTime()
    {
        ICacheService service = new NullCacheService();
        var factoryCallCount = 0;

        Task<Payload> Factory(CancellationToken _)
        {
            Interlocked.Increment(ref factoryCallCount);
            return Task.FromResult(new Payload { Value = "no-cache" });
        }

        await service.GetOrSetAsync("test:null:1", OneMinute, Factory, CancellationToken.None);
        var second = await service.GetOrSetAsync("test:null:1", OneMinute, Factory, CancellationToken.None);

        Assert.Equal(2, factoryCallCount);
        Assert.Equal("no-cache", second.Value);
    }

    private sealed class Payload
    {
        public string Value { get; set; } = string.Empty;
    }

    /// <summary>Redis'in tamamen erisilemez oldugu durumu taklit eder.</summary>
    private sealed class FailingDistributedCache : IDistributedCache
    {
        private static InvalidOperationException Failure() => new("redis unreachable");

        public byte[]? Get(string key) => throw Failure();

        public Task<byte[]?> GetAsync(string key, CancellationToken token = default) => throw Failure();

        public void Refresh(string key) => throw Failure();

        public Task RefreshAsync(string key, CancellationToken token = default) => throw Failure();

        public void Remove(string key) => throw Failure();

        public Task RemoveAsync(string key, CancellationToken token = default) => throw Failure();

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options) => throw Failure();

        public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default) => throw Failure();
    }
}
