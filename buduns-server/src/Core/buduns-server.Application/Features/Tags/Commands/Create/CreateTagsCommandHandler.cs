using buduns_server.Application.Common.Helpers;
using buduns_server.Application.Exceptions;
using buduns_server.Application.Repositories;
using buduns_server.Application.UnitOfWork;
using buduns_server.Domain.Entities;
using MediatR;

namespace buduns_server.Application.Features.Tags.Commands.Create
{
    public class CreateTagsCommandHandler : IRequestHandler<CreateTagsCommand, CreateTagsCommandResponse>
    {
        private readonly ITagRepository _tagRepository;
        private readonly IUnitOfWork _unitOfWork;

        public CreateTagsCommandHandler(ITagRepository tagRepository, IUnitOfWork unitOfWork)
        {
            _tagRepository = tagRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<CreateTagsCommandResponse> Handle(CreateTagsCommand request, CancellationToken cancellationToken)
        {
            var name = TagNameNormalizer.NormalizeDisplayName(request.Name);
            var normalizedName = TagNameNormalizer.NormalizeKey(request.Name);
            var exists = await _tagRepository.ExistsByNormalizedNameAsync(normalizedName, cancellationToken: cancellationToken);
            if (exists)
            {
                throw new BadRequestException("Bu tag zaten mevcut.");
            }

            var now = DateTime.UtcNow;
            var tag = new Tag { Name = name, NormalizedName = normalizedName, CreatedAt = now, UpdateAt = now, isActive = true, isDeleted = false };

            await _tagRepository.AddAsync(tag);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return new CreateTagsCommandResponse(Message: "Tag başarıyla eklendi.");
        }
    }
}
