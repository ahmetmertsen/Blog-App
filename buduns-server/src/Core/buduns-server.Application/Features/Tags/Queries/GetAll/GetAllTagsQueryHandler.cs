using buduns_server.Application.Dtos;
using buduns_server.Application.Repositories;
using MediatR;

namespace buduns_server.Application.Features.Tags.Queries.GetAll
{
    public class GetAllTagsQueryHandler : IRequestHandler<GetAllTagsQuery, PagedResponse<TagDto>>
    {
        private readonly ITagRepository _tagRepository;

        public GetAllTagsQueryHandler(ITagRepository tagRepository)
        {
            _tagRepository = tagRepository;
        }

        public async Task<PagedResponse<TagDto>> Handle(GetAllTagsQuery request, CancellationToken cancellationToken)
        {
            var result = await _tagRepository.GetPagedAsync(request.Page, request.Size, request.Search, cancellationToken);
            return new PagedResponse<TagDto> { Items = result.Items, Page = request.Page, Size = request.Size, TotalCount = result.TotalCount };
        }
    }
}
