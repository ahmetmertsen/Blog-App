using buduns_server.Application.Common.Helpers;
using buduns_server.Application.Exceptions;
using buduns_server.Application.UnitOfWork;
using MediatR;

namespace buduns_server.Application.Features.Tags.Commands.Update
{
    public class UpdateTagsCommandHandler : IRequestHandler<UpdateTagsCommand, UpdateTagsCommandResponse>
    {
        private readonly IUnitOfWork _unitOfWork;

        public UpdateTagsCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<UpdateTagsCommandResponse> Handle(UpdateTagsCommand request, CancellationToken cancellationToken)
        {
            var tag = await _unitOfWork.TagRepository.GetVisibleByIdAsync(request.Id, cancellationToken);
            if (tag == null)
            {
                throw new NotFoundException("Tag bulunamadı.");
            }

            var name = TagNameNormalizer.NormalizeDisplayName(request.Name);
            var normalizedName = TagNameNormalizer.NormalizeKey(request.Name);
            var exists = await _unitOfWork.TagRepository.ExistsByNormalizedNameAsync(normalizedName, request.Id, cancellationToken);
            if (exists)
            {
                throw new BadRequestException("Bu tag zaten mevcut.");
            }

            tag.Name = name;
            tag.NormalizedName = normalizedName;
            tag.UpdateAt = DateTime.UtcNow;

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return new UpdateTagsCommandResponse(Message: "Tag başarıyla güncellendi.");
        }
    }
}
