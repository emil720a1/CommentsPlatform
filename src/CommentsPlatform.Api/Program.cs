using CommentsPlatform.Api.Common.Cors;
using CommentsPlatform.Api.Common.Errors;
using CommentsPlatform.Application;
using CommentsPlatform.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

var corsSettings = builder.Configuration
    .GetSection(CorsSettings.SectionName)
    .Get<CorsSettings>()
    ?? new CorsSettings();

var allowedOrigins = corsSettings.AllowedOrigins
    .Where(origin => !string.IsNullOrWhiteSpace(origin))
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToArray();

builder.Services
    .AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
            ApiErrorMapper.Map(context.ModelState);
    });

builder.Services.AddOpenApi();
builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);

builder.Services.AddCors(options =>
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

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseCors(CorsPolicyNames.AngularFrontend);

app.MapControllers();

await app.RunAsync();

public partial class Program
{
}
