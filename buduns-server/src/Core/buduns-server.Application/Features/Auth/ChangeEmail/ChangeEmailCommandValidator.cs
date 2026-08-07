using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace buduns_server.Application.Features.Auth.ChangeEmail
{
    public class ChangeEmailCommandValidator : AbstractValidator<ChangeEmailCommand>
    {
        public ChangeEmailCommandValidator() 
        {
            RuleFor(x => x.NewEmail)
                .NotEmpty().WithMessage("Yeni Email bilgisi boş olamaz.")
                .EmailAddress().WithMessage("Yeni Email formatı geçersiz.");
        }
    }
}
