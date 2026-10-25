namespace MuseumExplorer.Api.Features;
public record Exhibition(string Id, string Slug, string Title, string Category, string Summary, string Description, string Gallery, int Minutes, string Colour, string[] Access);
// Fictional content for explicitly selected Sample mode and offline tests.
public static class Exhibitions
{
    public static readonly Exhibition[] All =
    [
        new("ocean", "a-world-beneath", "A world beneath", "Natural world",
            "Follow the currents into an extraordinary underwater world.",
            "Explore how light, pressure and movement shape life below the surface. This fictional exhibition brings ocean habitats into focus through stories of adaptation and discovery.",
            "Gallery 01", 25, "ocean", ["Step-free access", "Transcripts available"]),
        new("light", "shaping-light", "Shaping light", "Art & design",
            "Discover the colour, shadows and ideas hiding in everyday light.",
            "See familiar materials transformed by colour and shadow. This fictional exhibition explores how artists use light to change our understanding of everyday spaces.",
            "Gallery 02", 20, "light", ["Step-free access", "Seating available"]),
        new("earth", "the-restless-earth", "The restless earth", "Science",
            "Look closer at the forces that shape the ground beneath our feet.",
            "Trace the slow movement of landscapes and the sudden changes that reveal a planet in motion. This fictional exhibition introduces geological time through approachable visual stories.",
            "Gallery 03", 30, "earth", ["Step-free access", "Transcripts available"])
    ];
}
