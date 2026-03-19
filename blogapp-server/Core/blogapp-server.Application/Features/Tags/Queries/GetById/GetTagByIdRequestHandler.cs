using AutoMapper;
using blogapp_server.Application.Dtos;
using blogapp_server.Application.Exceptions;
using blogapp_server.Application.UnitOfWork;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Application.Features.Tags.Queries.GetById
{
    public class GetTagByIdRequestHandler : IRequestHandler<GetTagByIdRequest, TagDto>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public GetTagByIdRequestHandler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<TagDto> Handle(GetTagByIdRequest request, CancellationToken cancellationToken) 
        {
            var tag = await _unitOfWork.TagRepository.GetByIdAsync(request.Id);
            if (tag == null)
            {
                throw new NotFoundException("Tag bulunamadı!");
            }
            var response = _mapper.Map<TagDto>(tag);
            return response;
        }
    }
}
