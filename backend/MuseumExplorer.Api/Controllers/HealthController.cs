using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MuseumExplorer.Api.Data;

namespace MuseumExplorer.Api.Controllers;

[ApiController]
[Route("health")]
public class HealthController(MuseumDbContext db) : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new { status = "ok" });

    [HttpGet("ready")]
    public async Task<IActionResult> Ready(CancellationToken ct)
    {
        try
        {
            await db.Itineraries.AsNoTracking().Select(x => x.Id).Take(1).ToListAsync(ct);
            return Ok(new { status = "ready" });
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return Problem("The database is not ready. Check the connection and apply migrations.", statusCode: 503);
        }
    }
}
