using System.Text;
using buduns_server.IntegrationTests.Fixtures;

namespace buduns_server.IntegrationTests.Api;

/// <summary>
/// HTTP pipeline'inin sirasina bagli davranislari dogrular. Middleware sirasi
/// Program.cs icinde kolayca degistirilebilecek bir sey oldugu ve yanlis sira
/// sessizce calismaya devam ettigi icin bu davranislar teste baglandi.
/// </summary>
public sealed class HttpPipelineTests : IntegrationTestBase
{
    public HttpPipelineTests(BudunsWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Preflight_with_allowed_origin_should_return_cors_headers()
    {
        using var client = Factory.CreateHttpsClient();
        using var request = BuildPreflight(BudunsWebApplicationFactory.AllowedCorsOrigin);

        using var response = await client.SendAsync(request);

        response.Headers.GetValues("Access-Control-Allow-Origin")
            .Should().ContainSingle()
            .Which.Should().Be(BudunsWebApplicationFactory.AllowedCorsOrigin);
    }

    [Fact]
    public async Task Preflight_with_unknown_origin_should_not_return_cors_headers()
    {
        using var client = Factory.CreateHttpsClient();
        using var request = BuildPreflight("https://izinsiz.example");

        using var response = await client.SendAsync(request);

        response.Headers.Contains("Access-Control-Allow-Origin").Should().BeFalse();
    }

    /// <summary>
    /// Rate limit middleware'i CORS'tan SONRA calismalidir; aksi halde 429
    /// cevabi CORS basliklarini tasimaz ve tarayicida limit asimi yerine
    /// anlamsiz bir ag hatasi gorunur.
    ///
    /// Yol olarak /api/Auth/forgotPassword bilerek secildi: baska hicbir test
    /// bu yolu kullanmiyor. Rate limit sayaci process boyunca statik oldugu
    /// icin paylasilan bir yol kullanmak testleri birbirine bagimli kilardi.
    /// </summary>
    [Fact]
    public async Task Rate_limited_response_should_still_carry_cors_headers()
    {
        using var client = Factory.CreateHttpsClient();
        HttpResponseMessage? limitedResponse = null;

        for (var attempt = 0; attempt < 12 && limitedResponse is null; attempt++)
        {
            var response = await SendForgotPasswordAsync(client);
            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                limitedResponse = response;
            }
            else
            {
                response.Dispose();
            }
        }

        limitedResponse.Should().NotBeNull("yapilandirilan limit asildiginda 429 donmeliydi");

        using (limitedResponse)
        {
            limitedResponse!.Headers.GetValues("Access-Control-Allow-Origin")
                .Should().ContainSingle()
                .Which.Should().Be(BudunsWebApplicationFactory.AllowedCorsOrigin);
        }
    }

    private static HttpRequestMessage BuildPreflight(string origin)
    {
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/Auth/login");
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", "POST");
        return request;
    }

    private static Task<HttpResponseMessage> SendForgotPasswordAsync(HttpClient client)
    {
        // Govde bilerek gecersiz: amac endpoint'i calistirmak degil, rate
        // limit sayacini artirmak. Sayac middleware'de, routing'den once ve
        // sonuctan bagimsiz olarak isletiliyor.
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/Auth/forgotPassword")
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Origin", BudunsWebApplicationFactory.AllowedCorsOrigin);
        return client.SendAsync(request);
    }
}
