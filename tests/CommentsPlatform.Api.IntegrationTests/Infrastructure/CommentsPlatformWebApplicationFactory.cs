using CommentsPlatform.Application.Common.Abstractions.Security;
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
    public const string AllowedCorsOrigin = "http://localhost:4200";

    private readonly string _fileStorageRootPath = Path.Combine(
        Path.GetTempPath(),
        $"comments-platform-api-tests-{Guid.NewGuid():N}");

    private readonly MsSqlContainer _databaseContainer =
        new MsSqlBuilder(
                "mcr.microsoft.com/mssql/server:2022-CU14-ubuntu-22.04")
            .Build();

    private WebApplicationFactory<Program> _factory = null!;

    public IServiceProvider Services => _factory.Services;

    public string FileStorageRootPath => _fileStorageRootPath;

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
                builder.UseSetting(
                    "ConnectionStrings:DefaultConnection",
                    connectionString);

                builder.UseSetting(
                    "Cors:AllowedOrigins:0",
                    AllowedCorsOrigin);

                builder.UseSetting(
                    "CloudflareTurnstile:SecretKey",
                    "integration-test-secret");

                builder.UseSetting(
                    "FileStorage:RootPath",
                    _fileStorageRootPath);

                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<
                        DbContextOptions<ApplicationDbContext>>();
                    services.RemoveAll<ApplicationDbContext>();

                    services.AddDbContext<ApplicationDbContext>(options =>
                        options.UseSqlServer(connectionString));

                    services.RemoveAll<ICaptchaValidator>();
                    services.AddSingleton<
                        ICaptchaValidator,
                        FakeCaptchaValidator>();
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

        if (Directory.Exists(_fileStorageRootPath))
        {
            Directory.Delete(
                _fileStorageRootPath,
                recursive: true);
        }
    }
}
