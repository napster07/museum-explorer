using System.Security.Cryptography;
using System.Threading.RateLimiting;
using Microsoft.EntityFrameworkCore;
using MuseumExplorer.Api.Data;
using MuseumExplorer.Api.Features;
using MuseumExplorer.Api.Integrations.Sanity;
var builder = WebApplication.CreateBuilder(args);
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
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapGet("/health/ready", async (MuseumDbContext db, CancellationToken ct) =>
{
    try
    {
        await db.Itineraries.AsNoTracking().Select(x => x.Id).Take(1).ToListAsync(ct);
        return Results.Ok(new { status = "ready" });
    }
    catch (Exception ex) when (ex is not OperationCanceledException)
    {
        return Results.Problem("The database is not ready. Check the connection and apply migrations.", statusCode: 503);
    }
});
app.MapGet("/api/exhibitions", async (IExhibitionCatalogue catalogue, CancellationToken ct) => Results.Ok(await catalogue.GetAllAsync(ct)));
app.MapGet("/api/exhibitions/{slug}", async (string slug, IExhibitionCatalogue catalogue, CancellationToken ct) =>
    (await catalogue.GetAllAsync(ct)).FirstOrDefault(e => e.Slug == slug) is { } exhibition ? Results.Ok(exhibition) : Results.NotFound());
app.MapPost("/api/itineraries", async (CreateItineraryRequest request, MuseumDbContext db, IExhibitionCatalogue catalogue, HttpContext context, CancellationToken ct) =>
{
    if (ItineraryRules.Validate(request.ExhibitionIds) is { } error)
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["exhibitionIds"] = [error] });
    var available = await catalogue.GetAllAsync(ct);
    if (request.ExhibitionIds!.Any(id => !available.Any(exhibition => exhibition.Id == id)))
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["exhibitionIds"] = ["One or more exhibitions are unavailable."] });
    var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
    var itinerary = new Itinerary
    {
        Id = Guid.NewGuid(), AccessTokenHash = ItineraryRules.Hash(token),
        CreatedAt = DateTimeOffset.UtcNow, ExpiresAt = DateTimeOffset.UtcNow.AddDays(30),
        Stops = request.ExhibitionIds!.Select((id, position) => new ItineraryStop { ExhibitionId = id, Position = position }).ToList()
    };
    db.Itineraries.Add(itinerary);
    await db.SaveChangesAsync(ct);
    context.Response.Headers.CacheControl = "no-store";
    return Results.Created($"/api/itineraries/{itinerary.Id}", new { itinerary.Id, accessToken = token, itinerary.ExpiresAt });
}).RequireRateLimiting("writes");
app.MapGet("/api/itineraries/{id:guid}", async (Guid id, HttpContext context, MuseumDbContext db, CancellationToken ct) =>
{
    context.Response.Headers.CacheControl = "no-store";
    var authorization = context.Request.Headers.Authorization.ToString();
    if (!authorization.StartsWith("Bearer ", StringComparison.Ordinal) || authorization.Length != 71)
        return Results.Unauthorized();
    var itinerary = await db.Itineraries.AsNoTracking().Include(x => x.Stops).SingleOrDefaultAsync(x => x.Id == id, ct);
    if (itinerary is null || itinerary.ExpiresAt <= DateTimeOffset.UtcNow ||
        !ItineraryRules.Matches(authorization[7..], itinerary.AccessTokenHash)) return Results.NotFound();
    return Results.Ok(new { itinerary.Id, itinerary.CreatedAt, itinerary.ExpiresAt,
        exhibitionIds = itinerary.Stops.OrderBy(x => x.Position).Select(x => x.ExhibitionId) });
});
app.Run();
public partial class Program { }
