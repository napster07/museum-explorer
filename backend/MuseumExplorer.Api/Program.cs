using System.Threading.RateLimiting;
using Microsoft.EntityFrameworkCore;
using MuseumExplorer.Api.Data;
using MuseumExplorer.Api.Features;
using MuseumExplorer.Api.Integrations.Sanity;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ContentExceptionHandler>();
var contentProvider = builder.Configuration["Content:Provider"] ?? "Sample";
if (contentProvider == "Sanity")
{
    builder.Services.AddOptions<SanityOptions>()
        .Bind(builder.Configuration.GetSection("Sanity"))
        .Validate(options => options.IsValid(), "Configure a valid Sanity ProjectId, Dataset and Tag.")
        .ValidateOnStart();
    builder.Services.AddHttpClient<IExhibitionCatalogue, SanityExhibitionCatalogue>(client =>
        client.Timeout = TimeSpan.FromSeconds(10));
}
else if (contentProvider == "Sample")
{
    builder.Services.AddSingleton<IExhibitionCatalogue, SampleExhibitionCatalogue>();
}
else throw new InvalidOperationException("Content:Provider must be Sample or Sanity.");
builder.Services.AddDbContext<MuseumDbContext>(options => options.UseSqlServer(
    builder.Configuration.GetConnectionString("Museum") ?? throw new InvalidOperationException("Configure ConnectionStrings: Museum before starting the API.")));
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("writes", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 20, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});
var app = builder.Build();
app.UseExceptionHandler();
app.UseRateLimiter();
if (args.Contains("--migrate"))
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<MuseumDbContext>().Database.MigrateAsync();
    return;
}
app.MapControllers();
app.Run();
public partial class Program { }
