using buduns_server.Application.Common.Interfaces;
using buduns_server.Application.UnitOfWork;
using MediatR;

namespace buduns_server.Application.Common.Behaviors
{
    /// <summary>
    /// <see cref="ITransactionalRequest"/> isaretli isteklerin tum yazmalarini
    /// tek bir transaction'a alir. Sinir burada oldugu icin handler'lar
    /// transaction bilmez: commit'i unutmak diye bir hata sinifi kalmaz.
    /// <para>
    /// Pipeline'da en ice kaydedilir; boylece doğrulama ve hesap durumu
    /// kontrolleri sinirin disinda kalir.
    /// </para>
    /// </summary>
    public class TransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
    {
        private readonly IUnitOfWork _unitOfWork;

        public TransactionBehavior(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            if (request is not ITransactionalRequest)
            {
                return await next(cancellationToken);
            }

            return await _unitOfWork.ExecuteInTransactionAsync(token => next(token), cancellationToken);
        }
    }
}
