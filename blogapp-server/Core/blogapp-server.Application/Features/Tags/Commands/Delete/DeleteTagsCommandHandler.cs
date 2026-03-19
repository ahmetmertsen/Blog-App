using AutoMapper;
using blogapp_server.Application.Exceptions;
using blogapp_server.Application.UnitOfWork;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Application.Features.Tags.Commands.Delete
{
    public class DeleteTagsCommandHandler : IRequestHandler<DeleteTagsCommand,DeleteTagsCommandResponse>
    {
        private readonly IUnitOfWork _unitOfWork;
        public DeleteTagsCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<DeleteTagsCommandResponse> Handle(DeleteTagsCommand request, CancellationToken cancellationToken)
        {
            var tag = await _unitOfWork.TagRepository.GetByIdAsync(request.Id);
            if (tag == null)
            {
                throw new NotFoundException("Tag bulunamadı!");
            }

            await _unitOfWork.TagRepository.DeleteAsync(tag.Id);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return new DeleteTagsCommandResponse(Succeeded: true, Message: "Tag başarıyla silindi.");
        }

    }
}
