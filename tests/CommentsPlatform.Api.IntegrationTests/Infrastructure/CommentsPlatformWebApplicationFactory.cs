using CommentsPlatform.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.MsSql;

namespace CommentsPlatform.Api.IntegrationTests.Infrastructure;

public sealed class CommentsPlatformWebApplicationFactory : IAsyncLifetime
{
    private readonly MsSqlContainer _databaseContainer =
        new MsSqlBuilder(
                "mcr.microsoft.com/mssql/server:2022-CU14-ubuntu-22.04")
            .Build();

    private WebApplicationFactory<Program> _factory = null!;

    public IServiceProvider Services => _factory.Services;

    public HttpClient CreateClient()
    {
        return _factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost")
            });
    }

    public async Task InitializeAsync()
    {
        await _databaseContainer.StartAsync();

        var connectionStringBuilder = new SqlConnectionStringBuilder(
            connectionString: _databaseContainer.GetConnectionString())
        {
            TrustServerCertificate = true
        };

        var connectionString = connectionStringBuilder.ConnectionString;

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<
                        DbContextOptions<ApplicationDbContext>>();
                    services.RemoveAll<ApplicationDbContext>();

                    services.AddDbContext<ApplicationDbContext>(options =>
                        options.UseSqlServer(connectionString));
                });
            });

        using var scope = _factory.Services.CreateScope();

        var dbContext =
            scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        await dbContext.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        if (_factory is not null)
        {
            await _factory.DisposeAsync();
        }

        await _databaseContainer.DisposeAsync();
    }
}
