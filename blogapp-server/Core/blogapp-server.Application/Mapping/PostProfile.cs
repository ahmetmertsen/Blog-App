using AutoMapper;
using blogapp_server.Application.Dtos;
using blogapp_server.Application.Features.Posts.Commands.Create;
using blogapp_server.Application.Features.Posts.Commands.Update;
using blogapp_server.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Application.Mapping
{
    public class PostProfile : Profile
    {
        public PostProfile()
        {
            CreateMap<CreatePostsCommand, Post>()
                .ForMember(dest => dest.UserId, opt => opt.Ignore())
                .ForMember(dest => dest.Tags, opt => opt.Ignore());

            CreateMap<UpdatePostsCommand, Post>()
                .ForMember(dest => dest.UserId, opt => opt.Ignore())
                .ForMember(dest => dest.Tags, opt => opt.Ignore());

            CreateMap<Post, PostDto>()
                .ForMember(dest => dest.LikeCount,
                    opt => opt.MapFrom(src => src.Likes.Count))
                .ForMember(dest => dest.CommentCount,
                    opt => opt.MapFrom(src => src.Comments.Count))
                .ForMember(dest => dest.BookmarkCount,
                    opt => opt.MapFrom(src => src.Bookmarks.Count));
        }
    }
}
