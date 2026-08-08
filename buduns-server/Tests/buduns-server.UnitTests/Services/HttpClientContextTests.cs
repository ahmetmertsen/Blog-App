using System.Net;
using buduns_server.WebAPI.Services;
using Microsoft.AspNetCore.Http;

namespace buduns_server.UnitTests.Services;

/// <summary>
/// Oturum kayitlarindaki cihaz/IP bilgisi bu sinifla dolduruluyor; bos string
/// yerine null donmesi "Bilinmeyen cihaz" ayrimini mumkun kiliyor.
/// </summary>
public class HttpClientContextTests
{
    [Fact]
    public void Properties_WithHeadersAndConnection_ShouldReturnValues()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-Device-Name"] = "iPhone 15";
        httpContext.Request.Headers["User-Agent"] = "Mozilla/5.0";
        httpContext.Connection.RemoteIpAddress = IPAddress.Parse("203.0.113.4");
        var context = new HttpClientContext(new HttpContextAccessor { HttpContext = httpContext });

        Assert.Equal("iPhone 15", context.DeviceName);
        Assert.Equal("Mozilla/5.0", context.UserAgent);
        Assert.Equal("203.0.113.4", context.IpAddress);
    }

    [Fact]
    public void Properties_WithoutHeaders_ShouldReturnNull()
    {
        var context = new HttpClientContext(new HttpContextAccessor { HttpContext = new DefaultHttpContext() });

        Assert.Null(context.DeviceName);
        Assert.Null(context.UserAgent);
        Assert.Null(context.IpAddress);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Properties_BlankHeader_ShouldReturnNull(string headerValue)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-Device-Name"] = headerValue;
        var context = new HttpClientContext(new HttpContextAccessor { HttpContext = httpContext });

        Assert.Null(context.DeviceName);
    }

    [Fact]
    public void Properties_WithoutHttpContext_ShouldReturnNull()
    {
        // Arka plan islerinde HttpContext bulunmaz; erisim patlamamali.
        var context = new HttpClientContext(new HttpContextAccessor());

        Assert.Null(context.DeviceName);
        Assert.Null(context.UserAgent);
        Assert.Null(context.IpAddress);
    }

    [Fact]
    public void IpAddress_IPv6_ShouldBeReturnedInStringForm()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Connection.RemoteIpAddress = IPAddress.IPv6Loopback;
        var context = new HttpClientContext(new HttpContextAccessor { HttpContext = httpContext });

        Assert.Equal("::1", context.IpAddress);
    }
}
