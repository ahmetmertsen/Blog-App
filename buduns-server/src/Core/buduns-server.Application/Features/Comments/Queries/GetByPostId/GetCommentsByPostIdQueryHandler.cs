using buduns_server.Application.Dtos;
using buduns_server.Application.Exceptions;
using buduns_server.Application.UnitOfWork;
using MediatR;

namespace buduns_server.Application.Features.Comments.Queries.GetByPostId
{
    public class GetCommentsByPostIdQueryHandler : IRequestHandler<GetCommentsByPostIdQuery, PagedResponse<CommentDto>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetCommentsByPostIdQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<PagedResponse<CommentDto>> Handle(GetCommentsByPostIdQuery request, CancellationToken cancellationToken)
        {
            if (!await _unitOfWork.PostRepository.ExistsVisibleAsync(request.PostId, cancellationToken))
            {
                throw new NotFoundException("Paylaşım bulunamadı.");
            }

            var result = await _unitOfWork.CommentRepository.GetPagedByPostIdAsync(request.PostId, request.Page, request.Size, cancellationToken);
            return new PagedResponse<CommentDto> { Items = result.Items, Page = request.Page, Size = request.Size, TotalCount = result.TotalCount };
        }
    }
}
