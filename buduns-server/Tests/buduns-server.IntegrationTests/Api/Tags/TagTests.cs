using buduns_server.Application.Dtos;
using buduns_server.Application.Features.Tags.Commands.Create;
using buduns_server.Application.Features.Tags.Commands.Delete;
using buduns_server.Application.Features.Tags.Commands.Update;
using buduns_server.IntegrationTests.Fixtures;
using buduns_server.IntegrationTests.Helpers;
using buduns_server.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace buduns_server.IntegrationTests.Api.Tags;

/// <summary>
/// Tag benzersizligi NormalizedName uzerinden kuruluyor: "DotNet", "dotnet" ve
/// "  dotnet   " ayni etiketi ifade etmeli. Normalizasyon hem handler'da hem
/// veritabani indeksinde var; ikisinin ayni sonucu urettigi ancak gercek
/// veritabaniyla gorulur.
/// </summary>
public sealed class TagTests : IntegrationTestBase
{
    public TagTests(BudunsWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Create_tag_should_normalize_display_name_and_key()
    {
        var author = await CreateUserAsync("tag-creator");
        await GrantEndpointPermissionsAsync();
        using var authentication = await Factory.CreateAuthenticatedClientAsync(author.Id);

        var response = await authentication.Client.PostAsJsonAsync("/api/Tag/create", new CreateTagsCommand("  dotnet   core  "));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var stored = await Factory.ExecuteScopeAsync(async services =>
            await services.GetRequiredService<BudunsDbContext>().Tags.AsNoTracking().SingleAsync());
        stored.Name.Should().Be("dotnet core");
        stored.NormalizedName.Should().Be("DOTNET CORE");
    }

    [Fact]
    public async Task Create_tag_that_differs_only_by_case_or_spacing_should_be_rejected()
    {
        var author = await CreateUserAsync("tag-duplicate-creator");
        await GrantEndpointPermissionsAsync();
        using var authentication = await Factory.CreateAuthenticatedClientAsync(author.Id);
        (await authentication.Client.PostAsJsonAsync("/api/Tag/create", new CreateTagsCommand("dotnet"))).EnsureSuccessStatusCode();

        var upperCase = await authentication.Client.PostAsJsonAsync("/api/Tag/create", new CreateTagsCommand("DOTNET"));
        var extraSpaces = await authentication.Client.PostAsJsonAsync("/api/Tag/create", new CreateTagsCommand("  dotnet  "));

        upperCase.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        extraSpaces.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var tagCount = await Factory.ExecuteScopeAsync(async services => await services.GetRequiredService<BudunsDbContext>().Tags.CountAsync());
        tagCount.Should().Be(1);
    }

    [Fact]
    public async Task Update_tag_should_rename_it_and_keep_uniqueness()
    {
        var author = await CreateUserAsync("tag-updater");
        await GrantEndpointPermissionsAsync();
        var tag = await Factory.ExecuteScopeAsync(services => DatabaseSeeder.CreateTagAsync(services, "eski"));
        var other = await Factory.ExecuteScopeAsync(services => DatabaseSeeder.CreateTagAsync(services, "mevcut"));
        using var authentication = await Factory.CreateAuthenticatedClientAsync(author.Id);

        var conflicting = await authentication.Client.PutAsJsonAsync("/api/Tag/update", new UpdateTagsCommand(tag.Id, "MEVCUT"));
        var renamed = await authentication.Client.PutAsJsonAsync("/api/Tag/update", new UpdateTagsCommand(tag.Id, "yeni ad"));

        conflicting.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        renamed.StatusCode.Should().Be(HttpStatusCode.OK);
        var stored = await Factory.ExecuteScopeAsync(async services =>
            await services.GetRequiredService<BudunsDbContext>().Tags.AsNoTracking().SingleAsync(item => item.Id == tag.Id));
        stored.Name.Should().Be("yeni ad");
        stored.NormalizedName.Should().Be("YENI AD");
        other.Id.Should().NotBe(tag.Id);
    }

    [Fact]
    public async Task Update_tag_with_its_own_name_should_be_allowed()
    {
        var author = await CreateUserAsync("tag-self-updater");
        await GrantEndpointPermissionsAsync();
        var tag = await Factory.ExecuteScopeAsync(services => DatabaseSeeder.CreateTagAsync(services, "dotnet"));
        using var authentication = await Factory.CreateAuthenticatedClientAsync(author.Id);

        var response = await authentication.Client.PutAsJsonAsync("/api/Tag/update", new UpdateTagsCommand(tag.Id, "DotNet"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Delete_tag_should_soft_delete_it_and_remove_it_from_listings()
    {
        var author = await CreateUserAsync("tag-deleter");
        await GrantEndpointPermissionsAsync();
        var tag = await Factory.ExecuteScopeAsync(services => DatabaseSeeder.CreateTagAsync(services, "silinecek"));
        using var authentication = await Factory.CreateAuthenticatedClientAsync(author.Id);

        var response = await authentication.Client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, "/api/Tag/delete")
        {
            Content = JsonContent.Create(new DeleteTagsCommand(tag.Id))
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var stored = await Factory.ExecuteScopeAsync(async services =>
            await services.GetRequiredService<BudunsDbContext>().Tags.AsNoTracking().SingleAsync(item => item.Id == tag.Id));
        stored.isDeleted.Should().BeTrue();
        stored.isActive.Should().BeFalse();

        using var reader = Factory.CreateHttpsClient();
        (await reader.GetAsync($"/api/Tag/getById/{tag.Id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        var listing = await reader.GetFromJsonAsync<PagedResponse<TagDto>>("/api/Tag/getAll?page=1&size=50");
        listing!.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Delete_missing_tag_should_return_not_found()
    {
        var author = await CreateUserAsync("tag-missing-deleter");
        await GrantEndpointPermissionsAsync();
        using var authentication = await Factory.CreateAuthenticatedClientAsync(author.Id);

        var response = await authentication.Client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, "/api/Tag/delete")
        {
            Content = JsonContent.Create(new DeleteTagsCommand(999999))
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Tag_listing_should_be_public_searchable_and_report_post_counts()
    {
        var author = await CreateUserAsync("tag-count-author");
        var tag = await Factory.ExecuteScopeAsync(services => DatabaseSeeder.CreateTagAsync(services, "sayilan"));
        await Factory.ExecuteScopeAsync(services => DatabaseSeeder.CreateTagAsync(services, "digeri"));
        await Factory.ExecuteScopeAsync(async services =>
        {
            var post = await DatabaseSeeder.CreatePostAsync(services, author.Id);
            var context = services.GetRequiredService<BudunsDbContext>();
            var tracked = await context.Posts.Include(item => item.Tags).SingleAsync(item => item.Id == post.Id);
            tracked.Tags.Add(await context.Tags.SingleAsync(item => item.Id == tag.Id));
            await context.SaveChangesAsync();
        });
        using var client = Factory.CreateHttpsClient();

        var all = await client.GetFromJsonAsync<PagedResponse<TagDto>>("/api/Tag/getAll?page=1&size=50");
        var searched = await client.GetFromJsonAsync<PagedResponse<TagDto>>("/api/Tag/getAll?page=1&size=50&search=sayi");

        all!.TotalCount.Should().Be(2);
        all.Items.Single(item => item.Id == tag.Id).PostCount.Should().Be(1);
        all.Items.Single(item => item.Name == "digeri").PostCount.Should().Be(0);
        searched!.Items.Should().ContainSingle().Which.Id.Should().Be(tag.Id);
    }

    [Fact]
    public async Task Get_tag_by_id_should_be_public()
    {
        var tag = await Factory.ExecuteScopeAsync(services => DatabaseSeeder.CreateTagAsync(services, "acik"));
        using var client = Factory.CreateHttpsClient();

        var dto = await client.GetFromJsonAsync<TagDto>($"/api/Tag/getById/{tag.Id}");

        dto!.Name.Should().Be("acik");
    }

    [Fact]
    public async Task Tag_write_endpoints_should_reject_anonymous_requests()
    {
        using var client = Factory.CreateHttpsClient();

        (await client.PostAsJsonAsync("/api/Tag/create", new CreateTagsCommand("etiket"))).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await client.PutAsJsonAsync("/api/Tag/update", new UpdateTagsCommand(1, "etiket"))).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Tag_write_endpoints_should_reject_users_without_permission()
    {
        var author = await CreateUserAsync("tag-permissionless");
        using var authentication = await Factory.CreateAuthenticatedClientAsync(author.Id);

        var response = await authentication.Client.PostAsJsonAsync("/api/Tag/create", new CreateTagsCommand("etiket"));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
