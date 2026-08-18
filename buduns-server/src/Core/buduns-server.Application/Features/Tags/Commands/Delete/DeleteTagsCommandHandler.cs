using buduns_server.Application.Exceptions;
using buduns_server.Application.Repositories;
using buduns_server.Application.UnitOfWork;
using MediatR;

namespace buduns_server.Application.Features.Tags.Commands.Delete
{
    public class DeleteTagsCommandHandler : IRequestHandler<DeleteTagsCommand, DeleteTagsCommandResponse>
    {
        private readonly ITagRepository _tagRepository;
        private readonly IUnitOfWork _unitOfWork;

        public DeleteTagsCommandHandler(ITagRepository tagRepository, IUnitOfWork unitOfWork)
        {
            _tagRepository = tagRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<DeleteTagsCommandResponse> Handle(DeleteTagsCommand request, CancellationToken cancellationToken)
        {
            var tag = await _tagRepository.GetVisibleByIdAsync(request.Id, cancellationToken);
            if (tag == null)
            {
                throw new NotFoundException("Tag bulunamadı.");
            }

            tag.isActive = false;
            tag.isDeleted = true;
            tag.UpdateAt = DateTime.UtcNow;
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return new DeleteTagsCommandResponse(Message: "Tag başarıyla silindi.");
        }
    }
}
