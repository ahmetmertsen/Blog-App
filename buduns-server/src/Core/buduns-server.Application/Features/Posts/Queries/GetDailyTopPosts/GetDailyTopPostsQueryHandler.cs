using buduns_server.Application.Abstractions.Services;
using buduns_server.Application.Common.Consts;
using buduns_server.Application.Common.Options;
using buduns_server.Application.Dtos;
using buduns_server.Application.Repositories;
using MediatR;
using Microsoft.Extensions.Options;

namespace buduns_server.Application.Features.Posts.Queries.GetDailyTopPosts
{
    public class GetDailyTopPostsQueryHandler : IRequestHandler<GetDailyTopPostsQuery, List<TopPostDto>>
    {
        private const int TopPostLimit = 50;
        private static readonly TimeZoneInfo TurkeyTimeZone = ResolveTurkeyTimeZone();

        private readonly IPostRepository _postRepository;
        private readonly ICacheService _cacheService;
        private readonly CacheOptions _cacheOptions;

        public GetDailyTopPostsQueryHandler(IPostRepository postRepository, ICacheService cacheService, IOptions<CacheOptions> cacheOptions)
        {
            _postRepository = postRepository;
            _cacheService = cacheService;
            _cacheOptions = cacheOptions.Value;
        }

        public Task<List<TopPostDto>> Handle(GetDailyTopPostsQuery request, CancellationToken cancellationToken)
        {
            var nowInTurkey = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TurkeyTimeZone);
            var todayStartInTurkey = nowInTurkey.Date;
            var tomorrowStartInTurkey = todayStartInTurkey.AddDays(1);
            var startDateUtc = TimeZoneInfo.ConvertTimeToUtc(todayStartInTurkey, TurkeyTimeZone);
            var endDateUtc = TimeZoneInfo.ConvertTimeToUtc(tomorrowStartInTurkey, TurkeyTimeZone);

            return _cacheService.GetOrSetAsync(
                CacheKeys.DailyTopPosts(todayStartInTurkey, TopPostLimit),
                TimeSpan.FromSeconds(_cacheOptions.DailyTopPostsTtlSeconds),
                async token =>
                {
                    var topPosts = await _postRepository.GetDailyTopPostsAsync(startDateUtc, endDateUtc, TopPostLimit, token);

                    for (int i = 0; i < topPosts.Count; i++)
                    {
                        topPosts[i].Rank = i + 1;
                    }

                    return topPosts;
                },
                cancellationToken);
        }

        private static TimeZoneInfo ResolveTurkeyTimeZone()
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time");
            }
            catch (TimeZoneNotFoundException)
            {
                return TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");
            }
            catch (InvalidTimeZoneException)
            {
                return TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");
            }
        }
    }
}
