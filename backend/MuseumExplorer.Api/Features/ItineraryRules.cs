using System.Security.Cryptography;
using System.Text;
namespace MuseumExplorer.Api.Features;
public static class ItineraryRules
{
    public static string? Validate(string[]? ids)
    {
        if (ids is null || ids.Length is < 1 or > 12) return "Choose between 1 and 12 exhibitions.";
        if (ids.Distinct(StringComparer.Ordinal).Count() != ids.Length) return "Each exhibition may appear only once.";
        if (ids.Any(id => string.IsNullOrWhiteSpace(id) || id.Length > 100)) return "Exhibition IDs must be non-empty and at most 100 characters.";
        return null;
    }
    public static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    public static bool Matches(string token, string storedHash) => token.Length == 64 &&
        CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(Hash(token)), Encoding.ASCII.GetBytes(storedHash));
}
public record CreateItineraryRequest(string[]? ExhibitionIds);
