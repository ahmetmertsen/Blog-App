using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace buduns_server.Application.Features.AuthorizationEndpoint.Queries.GetRolesToEndpoint
{
    public class GetRolesToEndpointQueryValidator : AbstractValidator<GetRolesToEndpointQuery>  
    {
        public GetRolesToEndpointQueryValidator() 
        {
            RuleFor(r => r.Code)
                .NotEmpty().WithMessage("Code alanı boş olamaz.");
            RuleFor(r => r.Menu)
                .NotEmpty().WithMessage("Menu alanı boş olamaz.");
        }
    }
}
