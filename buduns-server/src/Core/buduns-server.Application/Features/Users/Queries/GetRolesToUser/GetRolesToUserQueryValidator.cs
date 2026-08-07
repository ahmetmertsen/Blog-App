using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace buduns_server.Application.Features.Users.Queries.GetRolesToUser
{
    public class GetRolesToUserQueryValidator : AbstractValidator<GetRolesToUserQuery>
    {
        public GetRolesToUserQueryValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("Kullanıcı bilgisi boş olamaz");
        }
    }
}
