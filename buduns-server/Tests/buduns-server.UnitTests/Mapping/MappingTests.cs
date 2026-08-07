using buduns_server.Application.Dtos.User;
using buduns_server.Application.Features.Auth.Register;
using buduns_server.Application.Features.Bookmarks.Commands.Create;
using buduns_server.Application.Features.Posts.Commands.Create;
using buduns_server.Application.Features.Posts.Commands.Update;
using buduns_server.Application.Mapping;
using buduns_server.Domain.Entities;
using buduns_server.Domain.Entities.Identity;
using buduns_server.Domain.Enums;

namespace buduns_server.UnitTests.Mapping;

/// <summary>
/// AutoMapper'dan elle mapping'e gecis sirasinda davranisin korundugunu
/// dogrular. Eski profillerde bu kurallar ForMember/Ignore ile ifade
/// ediliyordu ve hicbir test tarafindan kilitlenmemisti.
/// </summary>
public class MappingTests
{
    [Fact]
    public void RegisterUserCommand_ShouldMapAllFields()
    {
        var dto = new RegisterUserCommand("ahmet", "Ahmet Mert", "ahmet@test.com", "Secret123!").ToRequestDto();

        Assert.Equal("ahmet", dto.UserName);
        Assert.Equal("Ahmet Mert", dto.FullName);
        Assert.Equal("ahmet@test.com", dto.Email);
        Assert.Equal("Secret123!", dto.Password);
    }

    [Fact]
    public void RegisterUserRequestDto_ShouldNotCarryPasswordToEntity()
    {
        var user = new RegisterUserRequestDto { UserName = "ahmet", FullName = "Ahmet Mert", Email = "ahmet@test.com", Password = "Secret123!" }.ToEntity();

        Assert.Equal("ahmet", user.UserName);
        Assert.Equal("Ahmet Mert", user.FullName);
        Assert.Equal("ahmet@test.com", user.Email);
        // Parola UserManager.CreateAsync'e ayrica verilir, entity'ye tasinmaz.
        Assert.Null(user.PasswordHash);
    }

    [Fact]
    public void CreateBookmarksCommand_ShouldMapPostAndUser()
    {
        var bookmark = new CreateBookmarksCommand { PostId = 7, UserId = 3 }.ToEntity();

        Assert.Equal(7, bookmark.PostId);
        Assert.Equal(3, bookmark.UserId);
    }

    [Fact]
    public void CreatePostsCommand_ShouldMapOnlyContent()
    {
        var post = new CreatePostsCommand { UserId = 9, Content = "merhaba", TagIds = new List<int> { 1, 2 } }.ToEntity();

        Assert.Equal("merhaba", post.Content);
        // UserId ve Tags'i handler atar; mapping bunlara dokunmaz.
        Assert.Equal(0, post.UserId);
        Assert.Empty(post.Tags);
    }

    [Fact]
    public void UpdatePostsCommand_ShouldOnlyChangeContentOfExistingPost()
    {
        var post = new Post { Id = 5, UserId = 9, Content = "eski", isPublished = true, Status = PostStatus.Published };
        post.Tags.Add(new Tag { Id = 1, Name = "etiket", NormalizedName = "ETIKET" });

        new UpdatePostsCommand { Id = 5, Content = "yeni", UserId = 9, TagIds = new List<int> { 2 } }.ApplyTo(post);

        Assert.Equal("yeni", post.Content);
        Assert.Equal(5, post.Id);
        Assert.Equal(9, post.UserId);
        Assert.Single(post.Tags);
        Assert.True(post.isPublished);
    }

    [Fact]
    public void Follower_ShouldMapFollowingIdAsUserId()
    {
        var createdAt = new DateTime(2026, 3, 1, 12, 0, 0, DateTimeKind.Utc);

        var dto = new Follower { Id = 4, FollowerId = 10, FollowingId = 20, CreatedAt = createdAt }.ToDto();

        Assert.Equal(4, dto.Id);
        // Takip edilen kullanici tasinir, takip eden degil.
        Assert.Equal(20, dto.UserId);
        Assert.Equal(createdAt, dto.FollowedAt);
        Assert.Equal(string.Empty, dto.UserName);
    }

    [Fact]
    public void Like_ShouldTakeUserFieldsWhenNavigationIsLoaded()
    {
        var createdAt = new DateTime(2026, 3, 2, 0, 0, 0, DateTimeKind.Utc);
        var like = new Like { Id = 2, UserId = 11, CreatedAt = createdAt, User = new User { Id = 11, UserName = "ahmet", FullName = "Ahmet Mert", ImageUrl = "http://img" } };

        var dto = like.ToDto();

        Assert.Equal("ahmet", dto.UserName);
        Assert.Equal("Ahmet Mert", dto.FullName);
        Assert.Equal("http://img", dto.ImageUrl);
        Assert.Equal(createdAt, dto.LikedAt);
    }

    [Fact]
    public void Like_ShouldNotThrowWhenUserNavigationIsMissing()
    {
        // GetByIdAsync navigasyonu yuklemez; eski profil burada NullReference atardi.
        var dto = new Like { Id = 2, UserId = 11, User = null! }.ToDto();

        Assert.Equal(11, dto.UserId);
        Assert.Equal(string.Empty, dto.UserName);
        Assert.Null(dto.FullName);
    }

    [Fact]
    public void Role_ShouldMapIdAndName()
    {
        var dto = new Role { Id = 3, Name = "Admin" }.ToDto();

        Assert.Equal(3, dto.Id);
        Assert.Equal("Admin", dto.Name);
    }
}
