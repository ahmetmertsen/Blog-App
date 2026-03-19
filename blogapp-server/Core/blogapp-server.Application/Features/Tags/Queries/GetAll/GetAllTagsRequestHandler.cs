using AutoMapper;
using blogapp_server.Application.Dtos;
using blogapp_server.Application.UnitOfWork;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Application.Features.Tags.Queries.GetAll
{
    public class GetAllTagsRequestHandler : IRequestHandler<GetAllTagsRequest, List<TagDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public GetAllTagsRequestHandler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<List<TagDto>> Handle(GetAllTagsRequest request, CancellationToken cancellationToken)
        {
            var tags = await _unitOfWork.TagRepository.GetAllAsync();
            var response = _mapper.Map<List<TagDto>>(tags);
            return response;
        }
    }
}
