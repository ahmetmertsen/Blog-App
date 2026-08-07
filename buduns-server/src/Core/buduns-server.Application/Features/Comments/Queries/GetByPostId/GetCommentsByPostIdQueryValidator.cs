using FluentValidation;

namespace buduns_server.Application.Features.Comments.Queries.GetByPostId
{
    public class GetCommentsByPostIdQueryValidator : AbstractValidator<GetCommentsByPostIdQuery>
    {
        public GetCommentsByPostIdQueryValidator()
        {
            RuleFor(x => x.PostId).GreaterThan(0).WithMessage("Post Id 0'dan büyük olmalıdır.");
            RuleFor(x => x.Page).GreaterThanOrEqualTo(1).WithMessage("Sayfa numarası en az 1 olmalıdır.");
            RuleFor(x => x.Size).InclusiveBetween(1, 100).WithMessage("Sayfa boyutu 1 ile 100 arasında olmalıdır.");
        }
    }
}
