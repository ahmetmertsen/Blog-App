using buduns_server.IntegrationTests.Collections;

namespace buduns_server.IntegrationTests.Fixtures;

[Collection(IntegrationTestCollection.Name)]
public abstract class IntegrationTestBase : IAsyncLifetime
{
    protected IntegrationTestBase(BudunsWebApplicationFactory factory)
    {
        Factory = factory;
    }

    protected BudunsWebApplicationFactory Factory { get; }

    public Task InitializeAsync() => Factory.ResetStateAsync();

    public Task DisposeAsync() => Task.CompletedTask;
}
