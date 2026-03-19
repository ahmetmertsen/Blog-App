using AutoMapper;
using blogapp_server.Application.UnitOfWork;
using blogapp_server.Domain.Entities;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Application.Features.Tags.Commands.Create
{
    public class CreateTagsCommandHandler : IRequestHandler<CreateTagsCommand, CreateTagsCommandResponse>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public CreateTagsCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<CreateTagsCommandResponse> Handle(CreateTagsCommand request, CancellationToken cancellationToken)
        {
            var tag = _mapper.Map<Tag>(request);
            tag.isActive = true;
            tag.CreatedAt = DateTime.UtcNow;
            tag.isDeleted = false;

            await _unitOfWork.TagRepository.AddAsync(tag);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return new CreateTagsCommandResponse(Succeeded: true, Message: "Tag başarıyla eklendi.");
        }
    }
}
