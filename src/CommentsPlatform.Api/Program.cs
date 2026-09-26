using CommentsPlatform.Api.Common.Cors;
using CommentsPlatform.Api.Common.Errors;
using CommentsPlatform.Application;
using CommentsPlatform.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
            ApiErrorMapper.Map(context.ModelState);
    });

builder.Services.AddOpenApi();
builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);
builder.Services.AddConfiguredCors(builder.Configuration);
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
