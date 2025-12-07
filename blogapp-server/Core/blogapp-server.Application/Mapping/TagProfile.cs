using AutoMapper;
using blogapp_server.Application.Dtos;
using blogapp_server.Application.Features.Tags.Commands.Create;
using blogapp_server.Application.Features.Tags.Commands.Update;
using blogapp_server.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Application.Mapping
{
    public class TagProfile : Profile
    {
        public TagProfile() 
        {
            CreateMap<CreateTagsCommand, Tag>();
            CreateMap<UpdateTagsCommand, Tag>();

            CreateMap<Tag, TagDto>();
        }
    }
}
