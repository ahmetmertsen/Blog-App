using buduns_server.Application.Repositories;
using buduns_server.Application.UnitOfWork;
using buduns_server.Domain.Entities;
using buduns_server.IntegrationTests.Fixtures;
using buduns_server.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace buduns_server.IntegrationTests.Infrastructure.Database;

/// <summary>
/// Transaction sinirinin gercek veritabanindaki sozlesmesi. Kritik olan
/// ikinci test: FollowerRepository kendi SaveChangesAsync'ini cagiriyor, yani
/// sinir olmadan yazdigi satir hemen kalici oluyordu. Sinir acikken o cagri
/// artik commit degil, transaction icinde bir flush.
/// </summary>
public sealed class UnitOfWorkTransactionTests : IntegrationTestBase
{
    public UnitOfWorkTransactionTests(BudunsWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Failing_work_should_leave_nothing_behind()
    {
        var author = await CreateUserAsync("transaction-rollback-author");

        await Assert.ThrowsAsync<InvalidOperationException>(() => Factory.ExecuteScopeAsync(async services =>
        {
            var unitOfWork = services.GetRequiredService<IUnitOfWork>();
            var tagRepository = services.GetRequiredService<ITagRepository>();

            await unitOfWork.ExecuteInTransactionAsync<object?>(async token =>
            {
                await tagRepository.AddAsync(new Tag { Name = "geri-alinacak", NormalizedName = "GERI-ALINACAK", CreatedAt = DateTime.UtcNow, isActive = true, isDeleted = false });
                await unitOfWork.SaveChangesAsync(token);
                throw new InvalidOperationException("is yarida kaldi");
            }, CancellationToken.None);

            return (object?)null;
        }));

        var survived = await Factory.ExecuteScopeAsync(services =>
            services.GetRequiredService<BudunsDbContext>().Tags.AnyAsync(tag => tag.Name == "geri-alinacak"));

        survived.Should().BeFalse("transaction geri alindiysa etiket kalmamali");
        author.Id.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Repository_that_saves_on_its_own_should_still_be_rolled_back()
    {
        var follower = await CreateUserAsync("transaction-self-commit-follower");
        var following = await CreateUserAsync("transaction-self-commit-following");

        await Assert.ThrowsAsync<InvalidOperationException>(() => Factory.ExecuteScopeAsync(async services =>
        {
            var unitOfWork = services.GetRequiredService<IUnitOfWork>();
            var followerRepository = services.GetRequiredService<IFollowerRepository>();

            await unitOfWork.ExecuteInTransactionAsync<object?>(async token =>
            {
                // Bu cagri kendi SaveChangesAsync'ini atiyor.
                await followerRepository.CreateIfNotExistsAsync(
                    new Follower { FollowerId = follower.Id, FollowingId = following.Id, CreatedAt = DateTime.UtcNow, isActive = true, isDeleted = false },
                    token);

                throw new InvalidOperationException("is yarida kaldi");
            }, CancellationToken.None);

            return (object?)null;
        }));

        var survived = await Factory.ExecuteScopeAsync(services =>
            services.GetRequiredService<BudunsDbContext>().Followers
                .AnyAsync(item => item.FollowerId == follower.Id && item.FollowingId == following.Id));

        survived.Should().BeFalse("repository kendi commit'ini atsa da acik transaction geri almali");
    }

    [Fact]
    public async Task Successful_work_should_be_committed()
    {
        await Factory.ExecuteScopeAsync(async services =>
        {
            var unitOfWork = services.GetRequiredService<IUnitOfWork>();
            var tagRepository = services.GetRequiredService<ITagRepository>();

            return await unitOfWork.ExecuteInTransactionAsync(async token =>
            {
                await tagRepository.AddAsync(new Tag { Name = "kalici", NormalizedName = "KALICI", CreatedAt = DateTime.UtcNow, isActive = true, isDeleted = false });
                return await unitOfWork.SaveChangesAsync(token);
            }, CancellationToken.None);
        });

        var survived = await Factory.ExecuteScopeAsync(services =>
            services.GetRequiredService<BudunsDbContext>().Tags.AnyAsync(tag => tag.Name == "kalici"));

        survived.Should().BeTrue();
    }

    /// <summary>
    /// Ic ice cagri yeni transaction acmamali: EF ikinci BeginTransaction'da
    /// patlar. Dis sinir geri alinirsa ic isin yazdigi da gitmeli.
    /// </summary>
    [Fact]
    public async Task Nested_call_should_join_the_open_boundary()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => Factory.ExecuteScopeAsync(async services =>
        {
            var unitOfWork = services.GetRequiredService<IUnitOfWork>();
            var tagRepository = services.GetRequiredService<ITagRepository>();

            await unitOfWork.ExecuteInTransactionAsync<object?>(async outerToken =>
            {
                await unitOfWork.ExecuteInTransactionAsync<object?>(async innerToken =>
                {
                    await tagRepository.AddAsync(new Tag { Name = "ic-is", NormalizedName = "IC-IS", CreatedAt = DateTime.UtcNow, isActive = true, isDeleted = false });
                    await unitOfWork.SaveChangesAsync(innerToken);
                    return null;
                }, outerToken);

                throw new InvalidOperationException("dis is yarida kaldi");
            }, CancellationToken.None);

            return (object?)null;
        }));

        var survived = await Factory.ExecuteScopeAsync(services =>
            services.GetRequiredService<BudunsDbContext>().Tags.AnyAsync(tag => tag.Name == "ic-is"));

        survived.Should().BeFalse("ic cagri dis sinira katildiysa dis rollback onu da kapsamali");
    }
}
