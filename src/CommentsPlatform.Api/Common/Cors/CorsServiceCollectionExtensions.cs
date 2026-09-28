using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CommentsPlatform.Api.Common.Cors;

public static class CorsServiceCollectionExtensions
{
    public static IServiceCollection AddConfiguredCors(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var corsSettings = configuration
            .GetSection(CorsSettings.SectionName)
            .Get<CorsSettings>()
            ?? new CorsSettings();

        var allowedOrigins = corsSettings.AllowedOrigins
            .Where(origin => !string.IsNullOrWhiteSpace(origin))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        services.AddCors(options =>
        {
            options.AddPolicy(
                CorsPolicyNames.AngularFrontend,
                policy =>
                {
                    policy
                        .WithOrigins(allowedOrigins)
                        .WithMethods("GET", "POST", "OPTIONS")
                        .WithHeaders("Accept", "Content-Type");
                });
        });

        return services;
    }
}
