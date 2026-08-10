namespace buduns_server.Application.Abstractions.Services
{
    public interface IRoleSeeder
    {
        Task SeedAsync(CancellationToken cancellationToken);
    }
}
