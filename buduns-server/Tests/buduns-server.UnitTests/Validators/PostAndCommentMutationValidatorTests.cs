using buduns_server.Application.Features.Comments.Commands.Create;
using buduns_server.Application.Features.Comments.Commands.Update;
using buduns_server.Application.Features.Posts.Commands.Create;
using buduns_server.Application.Features.Posts.Commands.Update;

namespace buduns_server.UnitTests.Validators;

/// <summary>
/// ContentValidatorTests olusturma komutlarini kapsiyordu; guncelleme
/// komutlarinin ayni sinirlari tasidigini hicbir test dogrulamiyordu.
/// </summary>
public class PostAndCommentMutationValidatorTests
{
    [Fact]
    public async Task UpdatePost_ValidRequest_ShouldSucceed()
    {
        var result = await new UpdatePostsCommandValidator().ValidateAsync(new UpdatePostsCommand
        {
            Id = 5,
            Content = "Guncellenmis icerik",
            TagIds = new List<int> { 1, 2, 3 }
        });

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task UpdatePost_InvalidId_ShouldFail()
    {
        var result = await new UpdatePostsCommandValidator().ValidateAsync(new UpdatePostsCommand { Id = 0, Content = "icerik" });

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(UpdatePostsCommand.Id));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task UpdatePost_BlankContent_ShouldFail(string content)
    {
        var result = await new UpdatePostsCommandValidator().ValidateAsync(new UpdatePostsCommand { Id = 5, Content = content });

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(UpdatePostsCommand.Content));
    }

    [Fact]
    public async Task UpdatePost_ContentLengthBoundary_ShouldMatchLimit()
    {
        var atLimit = await new UpdatePostsCommandValidator().ValidateAsync(new UpdatePostsCommand { Id = 5, Content = new string('x', 1000) });
        var overLimit = await new UpdatePostsCommandValidator().ValidateAsync(new UpdatePostsCommand { Id = 5, Content = new string('x', 1001) });

        Assert.True(atLimit.IsValid);
        Assert.Contains(overLimit.Errors, error => error.PropertyName == nameof(UpdatePostsCommand.Content));
    }

    [Fact]
    public async Task UpdatePost_MoreThanThreeDistinctTags_ShouldFail()
    {
        var result = await new UpdatePostsCommandValidator().ValidateAsync(new UpdatePostsCommand
        {
            Id = 5,
            Content = "icerik",
            TagIds = new List<int> { 1, 2, 3, 4 }
        });

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(UpdatePostsCommand.TagIds));
    }

    [Fact]
    public async Task UpdatePost_RepeatedTagsCountAsOne_ShouldSucceed()
    {
        // Kural Distinct sayisina bakar; ayni tag tekrar gonderilebilir.
        var result = await new UpdatePostsCommandValidator().ValidateAsync(new UpdatePostsCommand
        {
            Id = 5,
            Content = "icerik",
            TagIds = new List<int> { 1, 1, 1, 1, 2 }
        });

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task UpdatePost_NonPositiveTagId_ShouldFail()
    {
        var result = await new UpdatePostsCommandValidator().ValidateAsync(new UpdatePostsCommand
        {
            Id = 5,
            Content = "icerik",
            TagIds = new List<int> { -1 }
        });

        Assert.Contains(result.Errors, error => error.PropertyName.StartsWith(nameof(UpdatePostsCommand.TagIds)));
    }

    [Fact]
    public async Task CreatePost_RepeatedTagsCountAsOne_ShouldSucceed()
    {
        var result = await new CreatePostsCommandValidator().ValidateAsync(new CreatePostsCommand
        {
            Content = "icerik",
            TagIds = new List<int> { 7, 7, 7, 7 }
        });

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task CreatePost_ContentAtLimit_ShouldSucceed()
    {
        var result = await new CreatePostsCommandValidator().ValidateAsync(new CreatePostsCommand { Content = new string('x', 1000) });

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task CreatePost_EmptyTagList_ShouldSucceed()
    {
        var result = await new CreatePostsCommandValidator().ValidateAsync(new CreatePostsCommand { Content = "icerik", TagIds = new List<int>() });

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task UpdateComment_ValidRequest_ShouldSucceed()
    {
        var result = await new UpdateCommentsCommandValidator().ValidateAsync(new UpdateCommentsCommand { Id = 3, Content = "Guncel yorum" });

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task UpdateComment_InvalidIdAndContent_ShouldFailBothProperties()
    {
        var result = await new UpdateCommentsCommandValidator().ValidateAsync(new UpdateCommentsCommand { Id = 0, Content = string.Empty });

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(UpdateCommentsCommand.Id));
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(UpdateCommentsCommand.Content));
    }

    [Fact]
    public async Task UpdateComment_ContentLengthBoundary_ShouldMatchLimit()
    {
        var atLimit = await new UpdateCommentsCommandValidator().ValidateAsync(new UpdateCommentsCommand { Id = 3, Content = new string('y', 1000) });
        var overLimit = await new UpdateCommentsCommandValidator().ValidateAsync(new UpdateCommentsCommand { Id = 3, Content = new string('y', 1001) });

        Assert.True(atLimit.IsValid);
        Assert.Contains(overLimit.Errors, error => error.PropertyName == nameof(UpdateCommentsCommand.Content));
    }

    [Fact]
    public async Task CreateComment_ContentLengthBoundary_ShouldMatchLimit()
    {
        var atLimit = await new CreateCommentsCommandValidator().ValidateAsync(new CreateCommentsCommand { PostId = 1, Content = new string('y', 1000) });
        var overLimit = await new CreateCommentsCommandValidator().ValidateAsync(new CreateCommentsCommand { PostId = 1, Content = new string('y', 1001) });

        Assert.True(atLimit.IsValid);
        Assert.Contains(overLimit.Errors, error => error.PropertyName == nameof(CreateCommentsCommand.Content));
    }
}
