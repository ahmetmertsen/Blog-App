using buduns_server.Application.Common.Consts;
using buduns_server.Application.Dtos;
using buduns_server.Application.Features.Auth.Login;
using buduns_server.IntegrationTests.Fixtures;
using buduns_server.IntegrationTests.Helpers;
using buduns_server.WebAPI.Models;

namespace buduns_server.IntegrationTests.Api;

public sealed class ApiContractTests : IntegrationTestBase
{
    public ApiContractTests(BudunsWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Successful_response_should_be_wrapped_in_the_global_envelope()
    {
        var user = await Factory.ExecuteScopeAsync(services => DatabaseSeeder.CreateUserAsync(services, "envelope-user", "Envelope User", RoleConstants.User));
        using var client = Factory.CreateHttpsClient();

        var response = await client.GetAsync($"/api/User/getUserById/{user.Id}");
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<UserDto>>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.Should().NotBeNull();
        body!.IsSuccess.Should().BeTrue();
        body.Error.Should().BeNull();
        body.TraceId.Should().NotBeNullOrWhiteSpace();
        body.Data.Should().NotBeNull();
        body.Data!.UserName.Should().Be("envelope-user");
    }

    [Fact]
    public async Task Protected_endpoint_without_token_should_return_unauthorized_envelope()
    {
        using var client = Factory.CreateHttpsClient();

        var response = await client.PostAsync("/api/Like/1", null);
        var error = await response.ReadErrorAsync();

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        error.Code.Should().Be("UNAUTHORIZED");
    }

    [Fact]
    public async Task Protected_endpoint_without_permission_should_return_forbidden_envelope()
    {
        var user = await Factory.ExecuteScopeAsync(services => DatabaseSeeder.CreateUserAsync(services, "regular-user", "Regular User", RoleConstants.User));
        using var authentication = await Factory.CreateAuthenticatedClientAsync(user.Id);

        var response = await authentication.Client.PostAsync("/api/Like/1", null);
        var error = await response.ReadErrorAsync();

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        error.Code.Should().Be("FORBIDDEN");
    }

    [Fact]
    public async Task Unknown_route_should_return_not_found_envelope()
    {
        using var client = Factory.CreateHttpsClient();

        var response = await client.GetAsync("/api/Boyle/Bir/Yol/Yok");
        var error = await response.ReadErrorAsync();

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        error.Code.Should().Be("NOT_FOUND");
    }

    [Fact]
    public async Task Model_binding_error_should_follow_global_error_contract()
    {
        using var client = Factory.CreateHttpsClient();

        // getUserById route'unda :int kisiti yok; "abc" route'a eslesir ama
        // model binding'de duser. Bu hata FluentValidation'a hic ulasmaz.
        var response = await client.GetAsync("/api/User/getUserById/abc");
        var error = await response.ReadErrorAsync();

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        error.Code.Should().Be("VALIDATION_ERROR");
        error.ValidationErrors.Should().ContainKey("userId");
    }

    [Fact]
    public async Task Validation_error_should_follow_global_error_contract()
    {
        using var client = Factory.CreateHttpsClient();

        var response = await client.PostAsJsonAsync("/api/Auth/login", new LoginUserCommand(string.Empty, string.Empty));
        var error = await response.ReadErrorAsync();

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        error.Code.Should().Be("VALIDATION_ERROR");
        error.ValidationErrors.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Not_found_error_should_follow_global_error_contract()
    {
        using var client = Factory.CreateHttpsClient();

        var response = await client.GetAsync("/api/User/getUserById/999999");
        var error = await response.ReadErrorAsync();

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        error.Code.Should().NotBeNullOrWhiteSpace();
    }
}
