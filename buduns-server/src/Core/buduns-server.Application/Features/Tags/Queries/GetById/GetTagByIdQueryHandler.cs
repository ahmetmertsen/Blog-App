using buduns_server.Application.Dtos;
using buduns_server.Application.Exceptions;
using buduns_server.Application.Repositories;
using MediatR;

namespace buduns_server.Application.Features.Tags.Queries.GetById
{
    public class GetTagByIdQueryHandler : IRequestHandler<GetTagByIdQuery, TagDto>
    {
        private readonly ITagRepository _tagRepository;

        public GetTagByIdQueryHandler(ITagRepository tagRepository)
        {
            _tagRepository = tagRepository;
        }

        public async Task<TagDto> Handle(GetTagByIdQuery request, CancellationToken cancellationToken)
        {
            var response = await _tagRepository.GetDtoByIdAsync(request.Id, cancellationToken);
            if (response == null)
            {
                throw new NotFoundException("Tag bulunamadı.");
            }

            return response;
        }
    }
}
