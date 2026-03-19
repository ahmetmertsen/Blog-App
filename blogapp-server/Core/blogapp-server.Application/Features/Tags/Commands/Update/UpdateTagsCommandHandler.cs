using AutoMapper;
using blogapp_server.Application.UnitOfWork;
using blogapp_server.Domain.Entities;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Application.Features.Tags.Commands.Update
{
    public class UpdateTagsCommandHandler : IRequestHandler<UpdateTagsCommand, UpdateTagsCommandResponse>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public UpdateTagsCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<UpdateTagsCommandResponse> Handle(UpdateTagsCommand request, CancellationToken cancellationToken)
        {
            var tag = await _unitOfWork.TagRepository.GetByIdAsync(request.Id);
            if (tag == null)
            {
                throw new DirectoryNotFoundException("Tag bulunamadı!");
            }
            _mapper.Map(request, tag);
            tag.UpdateAt = DateTime.UtcNow;

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return new UpdateTagsCommandResponse(Succeeded: true, Message: "Tag başarıyla güncellendi");
        }
    }
}
