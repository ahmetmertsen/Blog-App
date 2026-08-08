using buduns_server.Application.Helpers;

namespace buduns_server.UnitTests.Helpers;

public class CustomEncodersTests
{
    [Theory]
    [InlineData("ahmet@example.com")]
    [InlineData("Türkçe karakterler: ğüşiöçİ")]
    [InlineData("a")]
    [InlineData("")]
    public void UrlEncodeThenDecode_ShouldReturnOriginalValue(string value)
    {
        Assert.Equal(value, value.UrlEncode().UrlDecode());
    }

    [Fact]
    public void UrlEncode_ShouldNotProduceUrlUnsafeCharacters()
    {
        // Base64Url ciktisinda '+', '/' ve '=' bulunmamalidir; deger sorgu
        // dizesine kacislanmadan konuluyor.
        var encoded = new string('ö', 40).UrlEncode();

        Assert.DoesNotContain('+', encoded);
        Assert.DoesNotContain('/', encoded);
        Assert.DoesNotContain('=', encoded);
    }

    [Fact]
    public void UrlEncode_ShouldUseUtf8Bytes()
    {
        // 'ç' UTF-8'de iki bayt; kodlanmis deger ASCII karsiligindan farkli olmali.
        Assert.NotEqual("c".UrlEncode(), "ç".UrlEncode());
    }

    [Fact]
    public void UrlDecode_InvalidBase64_ShouldThrow()
    {
        Assert.ThrowsAny<FormatException>(() => "!!!".UrlDecode());
    }
}
