using buduns_server.WebAPI.Models;

namespace buduns_server.IntegrationTests.Helpers;

public static class ApiResponseHelper
{
    // Her basarili cevap ortak zarfla dondugu icin testler govdeyi dogrudan
    // okumaz; bu yardimci hem zarfi dogrular hem de payload'i acar.
    public static async Task<T> ReadDataAsync<T>(this HttpResponseMessage response)
    {
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<T>>();

        body.Should().NotBeNull();
        body!.IsSuccess.Should().BeTrue();
        body.Error.Should().BeNull();
        body.TraceId.Should().NotBeNullOrWhiteSpace();
        body.Data.Should().NotBeNull();

        return body.Data!;
    }

    // GetFromJsonAsync'in zarf farkindaki karsiligi: basarisiz status kodunda
    // atar, basarilida zarfi acar.
    public static async Task<T> GetDataAsync<T>(this HttpClient client, string requestUri)
    {
        using var response = await client.GetAsync(requestUri);
        response.EnsureSuccessStatusCode();

        return await response.ReadDataAsync<T>();
    }

    public static async Task<ErrorResponse> ReadErrorAsync(this HttpResponseMessage response)
    {
        var body = await response.Content.ReadFromJsonAsync<ApiResponse>();

        body.Should().NotBeNull();
        body!.IsSuccess.Should().BeFalse();
        body.TraceId.Should().NotBeNullOrWhiteSpace();
        body.Error.Should().NotBeNull();

        return body.Error!;
    }
}
