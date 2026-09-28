using CommentsPlatform.Api.Common.Cors;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CommentsPlatform.Api.UnitTests.Common.Cors;

public sealed class CorsServiceCollectionExtensionsTests
{
    [Fact]
    public void AddConfiguredCors_WithConfiguredOrigins_CreatesExpectedPolicy()
    {
        const string angularOrigin = "http://localhost:4200";
        const string productionOrigin = "https://comments.example.com";

        var configuration = CreateConfiguration(
            new Dictionary<string, string?>
            {
                ["Cors:AllowedOrigins:0"] = angularOrigin,
                ["Cors:AllowedOrigins:1"] = string.Empty,
                ["Cors:AllowedOrigins:2"] = angularOrigin.ToUpperInvariant(),
                ["Cors:AllowedOrigins:3"] = productionOrigin
            });

        var policy = CreatePolicy(configuration);

        Assert.Equal(2, policy.Origins.Count);
        Assert.Contains(angularOrigin, policy.Origins);
        Assert.Contains(productionOrigin, policy.Origins);
        Assert.Equal(["GET", "POST", "OPTIONS"], policy.Methods);
        Assert.Contains("Accept", policy.Headers);
        Assert.Contains("Content-Type", policy.Headers);
        Assert.False(policy.SupportsCredentials);
    }

    [Fact]
    public void AddConfiguredCors_WithoutConfiguredOrigins_CreatesClosedPolicy()
    {
        var configuration = CreateConfiguration(
            new Dictionary<string, string?>());

        var policy = CreatePolicy(configuration);

        Assert.Empty(policy.Origins);
        Assert.False(policy.AllowAnyOrigin);
        Assert.False(policy.SupportsCredentials);
    }

    private static IConfiguration CreateConfiguration(
        Dictionary<string, string?> values)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }

    private static CorsPolicy CreatePolicy(
        IConfiguration configuration)
    {
        var services = new ServiceCollection();

        services.AddConfiguredCors(configuration);

        using var serviceProvider = services.BuildServiceProvider();

        var corsOptions = serviceProvider
            .GetRequiredService<IOptions<CorsOptions>>()
            .Value;

        return corsOptions.GetPolicy(CorsPolicyNames.AngularFrontend)
               ?? throw new InvalidOperationException(
                   "The Angular frontend CORS policy was not registered.");
    }
}
