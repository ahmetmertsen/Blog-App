using System.Globalization;
using buduns_server.Application.Common.Consts;
using buduns_server.Application.Dtos;

namespace buduns_server.UnitTests.Helpers;

public class CacheKeyAndPagingTests
{
    [Fact]
    public void DailyTopPosts_ShouldContainDateAndLimit()
    {
        var key = CacheKeys.DailyTopPosts(new DateTime(2026, 8, 8, 13, 45, 0, DateTimeKind.Unspecified), 50);

        Assert.Equal("posts:daily-top:2026-08-08:50", key);
    }

    [Fact]
    public void DailyTopPosts_DifferentDays_ShouldProduceDifferentKeys()
    {
        // Gun donduğunde anahtar degismeli; aksi halde dunun listesi servis edilir.
        var today = CacheKeys.DailyTopPosts(new DateTime(2026, 8, 8), 50);
        var tomorrow = CacheKeys.DailyTopPosts(new DateTime(2026, 8, 9), 50);

        Assert.NotEqual(today, tomorrow);
    }

    [Fact]
    public void DailyTopPosts_DifferentLimits_ShouldProduceDifferentKeys()
    {
        Assert.NotEqual(CacheKeys.DailyTopPosts(new DateTime(2026, 8, 8), 50), CacheKeys.DailyTopPosts(new DateTime(2026, 8, 8), 10));
    }

    [Fact]
    public void DailyTopPosts_ShouldIgnoreCurrentCulture()
    {
        // Turkce kultur tarih ayraci olarak '.' kullanir; anahtar kulturden
        // bagimsiz olmali, yoksa sunucu kulturu degisince onbellek isabetsiz kalir.
        var originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("tr-TR");

            Assert.Equal("posts:daily-top:2026-08-08:50", CacheKeys.DailyTopPosts(new DateTime(2026, 8, 8), 50));
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Theory]
    [InlineData(0, 20, 0)]
    [InlineData(1, 20, 1)]
    [InlineData(20, 20, 1)]
    [InlineData(21, 20, 2)]
    [InlineData(51, 25, 3)]
    public void PagedResponse_TotalPages_ShouldRoundUp(int totalCount, int size, int expectedTotalPages)
    {
        var response = new PagedResponse<string> { TotalCount = totalCount, Size = size };

        Assert.Equal(expectedTotalPages, response.TotalPages);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void PagedResponse_NonPositiveSize_ShouldReturnZeroPagesInsteadOfDividingByZero(int size)
    {
        var response = new PagedResponse<string> { TotalCount = 100, Size = size };

        Assert.Equal(0, response.TotalPages);
    }

    [Fact]
    public void PagedResponse_ShouldStartWithEmptyItemList()
    {
        Assert.Empty(new PagedResponse<string>().Items);
    }
}
