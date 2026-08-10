namespace buduns_server.Application.Abstractions.Services
{
    public interface IAdminSeeder
    {
        Task SeedAsync(CancellationToken cancellationToken);
    }
}
