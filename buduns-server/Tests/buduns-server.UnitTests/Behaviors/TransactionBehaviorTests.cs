using buduns_server.Application.Common.Behaviors;
using buduns_server.Application.Common.Interfaces;
using buduns_server.Application.UnitOfWork;
using NSubstitute;

namespace buduns_server.UnitTests.Behaviors;

public class TransactionBehaviorTests
{
    [Fact]
    public async Task Handle_TransactionalRequest_ShouldRunTheHandlerInsideTheTransaction()
    {
        var unitOfWork = CreateUnitOfWorkThatRunsTheOperation();
        var behavior = new TransactionBehavior<TransactionalRequest, string>(unitOfWork);
        var handlerRan = false;

        var result = await behavior.Handle(new TransactionalRequest(), _ =>
        {
            handlerRan = true;
            return Task.FromResult("ok");
        }, CancellationToken.None);

        Assert.Equal("ok", result);
        Assert.True(handlerRan);
        await unitOfWork.Received(1).ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Sorgular ve muaf komutlar bos bir transaction acmamali; sinir yalnizca
    /// isaretli isteklerde kurulur.
    /// </summary>
    [Fact]
    public async Task Handle_PlainRequest_ShouldNotOpenATransaction()
    {
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var behavior = new TransactionBehavior<PlainRequest, string>(unitOfWork);

        var result = await behavior.Handle(new PlainRequest(), _ => Task.FromResult("ok"), CancellationToken.None);

        Assert.Equal("ok", result);
        await unitOfWork.DidNotReceiveWithAnyArgs().ExecuteInTransactionAsync(default(Func<CancellationToken, Task<string>>)!, default);
    }

    [Fact]
    public async Task Handle_HandlerThrows_ShouldLetTheExceptionReachTheTransactionOwner()
    {
        var unitOfWork = CreateUnitOfWorkThatRunsTheOperation();
        var behavior = new TransactionBehavior<TransactionalRequest, string>(unitOfWork);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            behavior.Handle(new TransactionalRequest(), _ => throw new InvalidOperationException("patladi"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ShouldPassTheCancellationTokenThrough()
    {
        using var cancellation = new CancellationTokenSource();
        var unitOfWork = CreateUnitOfWorkThatRunsTheOperation();
        var behavior = new TransactionBehavior<TransactionalRequest, string>(unitOfWork);
        CancellationToken seen = default;

        await behavior.Handle(new TransactionalRequest(), token =>
        {
            seen = token;
            return Task.FromResult("ok");
        }, cancellation.Token);

        Assert.Equal(cancellation.Token, seen);
    }

    // Gercek UnitOfWork isi calistirip commit ediyor; sahtenin de calistirmasi
    // gerekiyor, aksi halde handler hic kosmadan test yesil gorunurdu.
    private static IUnitOfWork CreateUnitOfWorkThatRunsTheOperation()
    {
        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<CancellationToken, Task<string>>>()(call.Arg<CancellationToken>()));
        return unitOfWork;
    }

    private sealed class TransactionalRequest : ITransactionalRequest
    {
    }

    private sealed class PlainRequest
    {
    }
}
