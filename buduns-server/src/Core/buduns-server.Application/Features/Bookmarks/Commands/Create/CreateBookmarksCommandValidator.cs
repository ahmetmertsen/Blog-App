using FluentValidation;

namespace buduns_server.Application.Features.Bookmarks.Commands.Create
{
    public class CreateBookmarksCommandValidator : AbstractValidator<CreateBookmarksCommand>
    {
        public CreateBookmarksCommandValidator()
        {
            RuleFor(x => x.PostId)
                .GreaterThan(0).WithMessage("Post Id 0'dan büyük olmalıdır.");
        }
    }
}
