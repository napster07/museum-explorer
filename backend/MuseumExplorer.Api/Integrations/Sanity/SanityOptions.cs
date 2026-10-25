using System.Text.RegularExpressions;

namespace MuseumExplorer.Api.Integrations.Sanity;

public sealed class SanityOptions
{
    public string ProjectId { get; set; } = "";
    public string Dataset { get; set; } = "production";
    public string Tag { get; set; } = "default";
    public string? ReadToken { get; set; }

    public bool IsValid() =>
        Regex.IsMatch(ProjectId, "^[a-z0-9]+$") &&
        Regex.IsMatch(Dataset, "^[a-z0-9_-]+$") &&
        Regex.IsMatch(Tag, "^[a-zA-Z0-9_-]+$");

    // Published perspective is explicit, including when using a private-dataset token.
    public Uri Endpoint => new(
        $"https://{ProjectId}.api.sanity.io/v2025-02-19/graphql/{Dataset}/{Tag}?perspective=published");
}
