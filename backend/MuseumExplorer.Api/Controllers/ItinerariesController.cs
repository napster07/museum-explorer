using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using MuseumExplorer.Api.Data;
using MuseumExplorer.Api.Features;

namespace MuseumExplorer.Api.Controllers;

[ApiController]
[Route("api/itineraries")]
public class ItinerariesController(MuseumDbContext db, IExhibitionCatalogue catalogue) : ControllerBase
{
    [HttpPost]
    [EnableRateLimiting("writes")]
    public async Task<IActionResult> Create(CreateItineraryRequest request, CancellationToken ct)
    {
        if (ItineraryRules.Validate(request.ExhibitionIds) is { } error)
            return ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]> { ["exhibitionIds"] = [error] }));
        var available = await catalogue.GetAllAsync(ct);
        if (request.ExhibitionIds!.Any(id => !available.Any(exhibition => exhibition.Id == id)))
            return ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]> { ["exhibitionIds"] = ["One or more exhibitions are unavailable."] }));
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var itinerary = new Itinerary
        {
            Id = Guid.NewGuid(), AccessTokenHash = ItineraryRules.Hash(token),
            CreatedAt = DateTimeOffset.UtcNow, ExpiresAt = DateTimeOffset.UtcNow.AddDays(30),
            Stops = request.ExhibitionIds!.Select((id, position) => new ItineraryStop { ExhibitionId = id, Position = position }).ToList()
        };
        db.Itineraries.Add(itinerary);
        await db.SaveChangesAsync(ct);
        Response.Headers.CacheControl = "no-store";
        return Created($"/api/itineraries/{itinerary.Id}", new { itinerary.Id, accessToken = token, itinerary.ExpiresAt });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        Response.Headers.CacheControl = "no-store";
        var authorization = Request.Headers.Authorization.ToString();
        if (!authorization.StartsWith("Bearer ", StringComparison.Ordinal) || authorization.Length != 71)
            return Unauthorized();
        var itinerary = await db.Itineraries.AsNoTracking().Include(x => x.Stops).SingleOrDefaultAsync(x => x.Id == id, ct);
        if (itinerary is null || itinerary.ExpiresAt <= DateTimeOffset.UtcNow ||
            !ItineraryRules.Matches(authorization[7..], itinerary.AccessTokenHash)) return NotFound();
        return Ok(new { itinerary.Id, itinerary.CreatedAt, itinerary.ExpiresAt,
            exhibitionIds = itinerary.Stops.OrderBy(x => x.Position).Select(x => x.ExhibitionId) });
    }
}
