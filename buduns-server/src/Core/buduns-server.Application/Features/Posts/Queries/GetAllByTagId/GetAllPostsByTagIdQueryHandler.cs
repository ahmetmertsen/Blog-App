using buduns_server.Application.Common.Helpers;
using buduns_server.Application.Dtos;
using buduns_server.Application.Repositories;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace buduns_server.Application.Features.Posts.Queries.GetAllByTagId
{
    public class GetAllPostsByTagIdQueryHandler : IRequestHandler<GetAllPostsByTagIdQuery, PagedResponse<PostDto>>
    {
        private readonly IPostRepository _postRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public GetAllPostsByTagIdQueryHandler(IPostRepository postRepository, IHttpContextAccessor httpContextAccessor)
        {
            _postRepository = postRepository;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<PagedResponse<PostDto>> Handle(GetAllPostsByTagIdQuery request, CancellationToken cancellationToken)
        {
            var viewerUserId = HttpContextUserHelper.GetUserId(_httpContextAccessor.HttpContext);
            var result = await _postRepository.GetPagedByTagIdAsync(request.TagId, request.Page, request.Size, viewerUserId, cancellationToken);
            return new PagedResponse<PostDto> { Items = result.Items, Page = request.Page, Size = request.Size, TotalCount = result.TotalCount };
        }
    }
}
