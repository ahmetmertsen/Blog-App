using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace buduns_server.Application.Features.Users.Commands.AssignRoleToUser
{
    public class AssignRoleToUserCommandValidator : AbstractValidator<AssignRoleToUserCommand>
    {
        public AssignRoleToUserCommandValidator()
        {
            RuleFor(x => x.TargetUserId).GreaterThan(0).WithMessage("Kullanıcı Id 0'dan büyük olmalıdır.");
            RuleFor(x => x.Roles).NotNull().NotEmpty().WithMessage("En az bir rol seçilmelidir.");
            RuleForEach(x => x.Roles).NotEmpty().MaximumLength(100).WithMessage("Rol adı boş olamaz ve en fazla 100 karakter olabilir.");
            RuleFor(x => x.Roles).Must(roles => roles == null || roles.Distinct(StringComparer.OrdinalIgnoreCase).Count() == roles.Length).WithMessage("Aynı rol birden fazla kez gönderilemez.");
        }
    }
}
