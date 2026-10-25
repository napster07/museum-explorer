using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace MuseumExplorer.Api.Integrations.Sanity;

public sealed class ContentExceptionHandler(ILogger<ContentExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not ContentUnavailableException) return false;
        // Never log the upstream body or request headers: these may contain confidential content.
        logger.LogWarning("The content provider is unavailable.");
        context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        context.Response.Headers.CacheControl = "no-store";
        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = 503,
            Title = "Exhibition content is temporarily unavailable",
            Detail = "Please try again shortly."
        }, cancellationToken);
        return true;
    }
}
