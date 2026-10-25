using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using MuseumExplorer.Api.Features;

namespace MuseumExplorer.Api.Integrations.Sanity;

public sealed class ContentUnavailableException(string message, Exception? inner = null) : Exception(message, inner);

public sealed class SanityExhibitionCatalogue(HttpClient client, IOptions<SanityOptions> options)
    : IExhibitionCatalogue
{
    private const int PageSize = 100;
    internal const string Query = """
        query Exhibitions($limit: Int!, $offset: Int!) {
          allExhibition(limit: $limit, offset: $offset, sort: [{_id: ASC}]) {
            _id
            slug { current }
            title
            category
            summary
            description
            gallery
            minutes
            colour
            access
          }
        }
        """;

    public async Task<IReadOnlyList<Exhibition>> GetAllAsync(CancellationToken cancellationToken)
    {
        try
        {
            var result = new List<Exhibition>();
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var slugs = new HashSet<string>(StringComparer.Ordinal);
            for (var offset = 0; offset < 10000; offset += PageSize)
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, options.Value.Endpoint);
                request.Content = JsonContent.Create(new { query = Query, variables = new { limit = PageSize, offset } });
                if (!string.IsNullOrWhiteSpace(options.Value.ReadToken))
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.Value.ReadToken);
                using var response = await client.SendAsync(request, cancellationToken);
                if (!response.IsSuccessStatusCode)
                    throw new ContentUnavailableException($"Sanity returned HTTP {(int)response.StatusCode}.");
                using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
                var root = payload.RootElement;
                if (root.TryGetProperty("errors", out var errors) &&
                    (errors.ValueKind != JsonValueKind.Array || errors.GetArrayLength() > 0))
                    throw new ContentUnavailableException("Sanity returned GraphQL errors.");
                if (!root.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Object ||
                    !data.TryGetProperty("allExhibition", out var documents) || documents.ValueKind != JsonValueKind.Array)
                    throw new ContentUnavailableException("Sanity returned an unexpected response.");

                foreach (var document in documents.EnumerateArray())
                {
                    var id = RequiredString(document, "_id");
                    // Defence in depth: never serve drafts or content release versions.
                    if (id.StartsWith("drafts.", StringComparison.Ordinal) || id.StartsWith("versions.", StringComparison.Ordinal))
                        continue;
                    var exhibition = Map(document, id);
                    if (!ids.Add(id) || !slugs.Add(exhibition.Slug))
                        throw new ContentUnavailableException("Sanity catalogue has duplicate IDs or slugs.");
                    result.Add(exhibition);
                }
                if (documents.GetArrayLength() < PageSize) return result;
            }
            throw new ContentUnavailableException("Sanity catalogue exceeded its supported size.");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new ContentUnavailableException("Sanity request timed out.");
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or InvalidOperationException or KeyNotFoundException)
        {
            throw new ContentUnavailableException("Could not read the Sanity catalogue.", ex);
        }
    }

    private static Exhibition Map(JsonElement document, string id)
    {
        var slug = RequiredString(document.GetProperty("slug"), "current");
        var category = RequiredString(document, "category");
        var colour = RequiredString(document, "colour");
        var minutes = document.GetProperty("minutes").GetDouble();
        if (id.Length > 100 || minutes < 1 || minutes > 240 || minutes != Math.Truncate(minutes) ||
            !new[] { "Natural world", "Art & design", "Science" }.Contains(category) ||
            !new[] { "ocean", "light", "earth" }.Contains(colour))
            throw new ContentUnavailableException("An exhibition has invalid values.");
        var access = document.TryGetProperty("access", out var accessValue) && accessValue.ValueKind == JsonValueKind.Array
            ? accessValue.EnumerateArray().Select(x => x.GetString() ?? "").Where(x => x.Length > 0).ToArray()
            : [];
        return new Exhibition(id, slug, RequiredString(document, "title"), category,
            RequiredString(document, "summary"), RequiredString(document, "description"),
            RequiredString(document, "gallery"), (int)minutes, colour, access);
    }

    private static string RequiredString(JsonElement document, string field)
    {
        var value = document.GetProperty(field).GetString();
        return !string.IsNullOrWhiteSpace(value) ? value :
            throw new ContentUnavailableException($"An exhibition is missing a required field: {field}.");
    }
}
