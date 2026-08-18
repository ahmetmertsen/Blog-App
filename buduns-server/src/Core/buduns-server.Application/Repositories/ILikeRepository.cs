using buduns_server.Application.Dtos;
using buduns_server.Application.Repositories.Common;
using buduns_server.Domain.Entities;

namespace buduns_server.Application.Repositories
{
    public interface ILikeRepository : IRepository<Like>
    {
        Task<(Like Like, bool Created)> CreateIfNotExistsAsync(Like like, CancellationToken cancellationToken);
        Task<bool> DeleteByUserAndPostAsync(int userId, int postId, CancellationToken cancellationToken);
        Task<Like?> GetByUserAndPostAsync(int userId, int postId, CancellationToken cancellationToken);
        Task<(List<LikeDto> Items, int TotalCount)> GetPagedByPostIdAsync(int postId, int page, int size, CancellationToken cancellationToken);
        Task<(List<LikedPostDto> Items, int TotalCount)> GetPagedByUserIdAsync(int userId, int page, int size, CancellationToken cancellationToken);
    }
}
