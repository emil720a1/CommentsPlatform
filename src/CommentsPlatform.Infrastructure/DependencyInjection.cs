using CommentsPlatform.Application.Common.Abstractions.Persistence;
using CommentsPlatform.Application.Common.Abstractions.Security;
using CommentsPlatform.Infrastructure.Persistence;
using CommentsPlatform.Infrastructure.Persistence.Repositories;
using CommentsPlatform.Infrastructure.Security.CloudflareTurnstile;
using CommentsPlatform.Infrastructure.Security.HtmlSanitization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CommentsPlatform.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString =
            configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'DefaultConnection' was not found.");
        }

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddScoped<ICommentRepository, CommentRepository>();
        services.AddSingleton<IHtmlSanitizer, HtmlSanitizerService>();

        services.AddOptions<CloudflareTurnstileOptions>()
            .Bind(configuration.GetSection(
                CloudflareTurnstileOptions.SectionName))
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.SecretKey),
                "Cloudflare Turnstile secret key is required.")
            .Validate(
                options =>
                    Uri.TryCreate(
                        options.VerificationUrl,
                        UriKind.Absolute,
                        out var verificationUri) &&
                    verificationUri.Scheme == Uri.UriSchemeHttps,
                "Cloudflare Turnstile verification URL must use HTTPS.")
            .Validate(
                options => options.TimeoutSeconds > 0,
                "Cloudflare Turnstile timeout must be greater than zero.")
            .ValidateOnStart();

        services.AddHttpClient<
            ICaptchaValidator,
            CloudflareTurnstileValidator>();

        return services;
    }
}
