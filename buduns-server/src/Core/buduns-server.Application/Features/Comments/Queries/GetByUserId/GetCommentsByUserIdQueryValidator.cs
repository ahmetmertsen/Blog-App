using FluentValidation;

namespace buduns_server.Application.Features.Comments.Queries.GetByUserId
{
    public class GetCommentsByUserIdQueryValidator : AbstractValidator<GetCommentsByUserIdQuery>
    {
        public GetCommentsByUserIdQueryValidator()
        {
            RuleFor(x => x.UserId).GreaterThan(0).WithMessage("User Id 0'dan büyük olmalıdır.");
            RuleFor(x => x.Page).GreaterThanOrEqualTo(1).WithMessage("Sayfa numarası en az 1 olmalıdır.");
            RuleFor(x => x.Size).InclusiveBetween(1, 100).WithMessage("Sayfa boyutu 1 ile 100 arasında olmalıdır.");
        }
    }
}
