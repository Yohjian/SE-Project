using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QuattroLingo.Data;

namespace QuattroLingo.Tests.Integration;

public class QuattroLingoFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = Guid.NewGuid().ToString();

    static QuattroLingoFactory()
    {
        Environment.SetEnvironmentVariable("Jwt__Key", "end-to-end-test-signing-key-at-least-32-bytes!");
        Environment.SetEnvironmentVariable("Jwt__Issuer", "quattrolingo-tests");
        Environment.SetEnvironmentVariable("Jwt__Audience", "quattrolingo-tests");
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", "Host=unused;Database=unused");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var toRemove = services
                .Where(d =>
                    d.ServiceType == typeof(DbContextOptions<AppDbContext>) ||
                    (d.ServiceType.IsGenericType &&
                     d.ServiceType.Name.StartsWith("IDbContextOptionsConfiguration") &&
                     d.ServiceType.GetGenericArguments().Contains(typeof(AppDbContext))))
                .ToList();

            foreach (var descriptor in toRemove)
                services.Remove(descriptor);

            services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(_dbName));
        });
    }
}