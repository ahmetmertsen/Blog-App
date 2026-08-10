using buduns_server.Application.Common.Consts;
using buduns_server.Application.Dtos;
using buduns_server.Application.Dtos.User;
using buduns_server.Domain.Enums;
using buduns_server.IntegrationTests.Fixtures;
using buduns_server.IntegrationTests.Helpers;

namespace buduns_server.IntegrationTests.Api.Users;

public sealed class UserQueryTests : IntegrationTestBase
{
    public UserQueryTests(BudunsWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Admin_user_query_should_apply_pagination_search_and_status_filters()
    {
        var admin = await Factory.ExecuteScopeAsync(services => DatabaseSeeder.CreateUserAsync(services, "query-admin", "Query Admin", RoleConstants.Admin));
        await Factory.ExecuteScopeAsync(services => DatabaseSeeder.CreateUserAsync(services, "alice-active", "Alice Searchable", RoleConstants.User));
        await Factory.ExecuteScopeAsync(services => DatabaseSeeder.CreateUserAsync(services, "bob-active", "Bob User", RoleConstants.User));
        using var authentication = await Factory.CreateAuthenticatedClientAsync(admin.Id);
        var response = await authentication.Client.GetAsync("/api/User/getAllUsers?page=1&size=1&search=Alice&status=Active&emailConfirmed=true");
        var body = await response.ReadDataAsync<PagedResponse<AdminUserDto>>();
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body!.Page.Should().Be(1);
        body.Size.Should().Be(1);
        body.TotalCount.Should().Be(1);
        body.Items.Should().ContainSingle(item => item.UserName == "alice-active" && item.Status == UserStatus.Active && item.EmailConfirmed);
    }
}
