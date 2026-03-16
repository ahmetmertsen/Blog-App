using AutoMapper;
using blogapp_server.Application.Dtos;
using blogapp_server.Application.Features.Auth.Register;
using blogapp_server.Application.Features.Users.Commands.Update.UpdateEmail;
using blogapp_server.Application.Features.Users.Commands.Update.UpdatePassword;
using blogapp_server.Application.Features.Users.Commands.Update.UpdatePhoneNumber;
using blogapp_server.Application.Features.Users.Commands.Update.UpdateProfile;
using blogapp_server.Domain.Entities.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Application.Mapping
{
    public class UserProfile : Profile
    {
        public UserProfile() 
        {
            //Register
            CreateMap<RegisterUserCommand, User>();
            CreateMap<RegisterUserCommand, RegisterUserRequestDto>();
            CreateMap<RegisterUserRequestDto, User>();
            CreateMap<User, RegisterUserCommandResponse>();

            //Update
            CreateMap<UpdateUserProfileCommand, User>();
            CreateMap<UpdateUserEmailCommand, User>();
            CreateMap<UpdateUserPasswordCommand, User>();
            CreateMap<UpdateUserPhoneNumberCommand, User>();

            //Dto
            CreateMap<User, UserDto>();
        }
    }
}
