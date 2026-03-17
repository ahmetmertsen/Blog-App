using AutoMapper;
using blogapp_server.Application.Exceptions;
using blogapp_server.Application.UnitOfWork;
using blogapp_server.Domain.Entities;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Application.Features.Comments.Commands.Update
{
    public class UpdateCommentsCommandHandler : IRequestHandler<UpdateCommentsCommand, UpdateCommentsCommandResponse>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public UpdateCommentsCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<UpdateCommentsCommandResponse> Handle(UpdateCommentsCommand request, CancellationToken cancellationToken)
        {
            var comment = await _unitOfWork.CommentRepository.GetByIdAsync(request.Id);
            if (comment == null)
            {
                throw new NotFoundException("Yorum bulunamadı!");
            }
            if (comment.UserId != request.UserId)
            {
                throw new UnauthorizedAccesException("Bu yorumu güncelleme yetkiniz yok.");
            }
            _mapper.Map(request, comment);
            comment.UpdateAt = DateTime.UtcNow;
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return new UpdateCommentsCommandResponse(Succeeded: true, Message: "Mesaj başarıyla güncellendi");
        }
    }
}
