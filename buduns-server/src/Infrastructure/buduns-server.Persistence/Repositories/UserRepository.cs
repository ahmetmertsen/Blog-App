using buduns_server.Application.Repositories;
using buduns_server.Domain.Entities.Identity;
using buduns_server.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace buduns_server.Persistence.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly BudunsDbContext _context;

        public UserRepository(BudunsDbContext context)
        {
            _context = context;
        }

        // Izlemeli okuma: UserManager.FindByIdAsync da boyle davraniyordu,
        // AccountStatusBehavior askiyi kaldirirken ayni ornegi guncelliyor.
        public Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
            => _context.Users.FirstOrDefaultAsync(user => user.Id == id, cancellationToken);

        public void Update(User user) => _context.Users.Update(user);
    }
}
