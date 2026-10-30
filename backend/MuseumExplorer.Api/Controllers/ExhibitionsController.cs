using Microsoft.AspNetCore.Mvc;
using MuseumExplorer.Api.Features;

namespace MuseumExplorer.Api.Controllers;

[ApiController]
[Route("api/exhibitions")]
public class ExhibitionsController(IExhibitionCatalogue catalogue) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct) => Ok(await catalogue.GetAllAsync(ct));

    [HttpGet("{slug}")]
    public async Task<IActionResult> GetBySlug(string slug, CancellationToken ct) =>
        (await catalogue.GetAllAsync(ct)).FirstOrDefault(e => e.Slug == slug) is { } exhibition ? Ok(exhibition) : NotFound();
}
