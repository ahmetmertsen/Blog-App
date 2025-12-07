using AutoMapper;
using blogapp_server.Application.Dtos;
using blogapp_server.Application.Features.Bookmarks.Commands.Create;
using blogapp_server.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Application.Mapping
{
    public class BookmarkProfile : Profile
    {
        public BookmarkProfile()
        {
            CreateMap<CreateBookmarksCommand, Bookmark>();
            CreateMap<Bookmark, BookmarkDto>();
        }
    }
}
