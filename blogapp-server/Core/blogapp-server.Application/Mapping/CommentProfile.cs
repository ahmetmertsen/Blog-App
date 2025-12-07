using AutoMapper;
using blogapp_server.Application.Dtos;
using blogapp_server.Application.Features.Comments.Commands.Create;
using blogapp_server.Application.Features.Comments.Commands.Update;
using blogapp_server.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Application.Mapping
{
    public class CommentProfile : Profile
    {
        public CommentProfile()
        {
            CreateMap<CreateCommentsCommand, Comment>();
            CreateMap<UpdateCommentsCommand, Comment>();

            CreateMap<Comment, CommentDto>();
        }
    }
}
