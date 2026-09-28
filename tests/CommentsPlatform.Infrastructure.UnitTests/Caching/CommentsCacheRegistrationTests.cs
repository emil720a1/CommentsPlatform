using CommentsPlatform.Application.Features.Comments.Queries.GetComments;
using CommentsPlatform.Infrastructure.Caching;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CommentsPlatform.Infrastructure.UnitTests.Caching;

public sealed class CommentsCacheRegistrationTests
{
    [Fact]
    public void AddInfrastructure_WhenCachingIsEnabled_RegistersMemoryCache()
    {
        var services = new ServiceCollection();
        var configuration = CreateConfiguration(cacheEnabled: true);

        services.AddInfrastructure(configuration);

        var cacheDescriptor = Assert.Single(
            services,
            service =>
                service.ServiceType == typeof(ICommentsQueryCache));

        Assert.Equal(
            typeof(MemoryCommentsQueryCache),
            cacheDescriptor.ImplementationType);
    }

    [Fact]
    public void AddInfrastructure_WhenCachingIsDisabled_RegistersNoOpCache()
    {
        var services = new ServiceCollection();
        var configuration = CreateConfiguration(cacheEnabled: false);

        services.AddInfrastructure(configuration);

        var cacheDescriptor = Assert.Single(
            services,
            service =>
                service.ServiceType == typeof(ICommentsQueryCache));

        Assert.Equal(
            typeof(NoOpCommentsQueryCache),
            cacheDescriptor.ImplementationType);
    }

    private static IConfiguration CreateConfiguration(
        bool cacheEnabled)
    {
        var settings = new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] =
                "Server=localhost;Database=CommentsPlatform;User Id=sa;Password=Test_password123;TrustServerCertificate=True",
            ["CommentsCache:Enabled"] = cacheEnabled.ToString(),
            ["CommentsCache:DurationSeconds"] = "60",
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();
    }
}
