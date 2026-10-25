using Microsoft.EntityFrameworkCore;
namespace MuseumExplorer.Api.Data;
public class MuseumDbContext(DbContextOptions<MuseumDbContext> options) : DbContext(options)
{
    public DbSet<Itinerary> Itineraries => Set<Itinerary>();
    public DbSet<ItineraryStop> ItineraryStops => Set<ItineraryStop>();
    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<Itinerary>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.AccessTokenHash).HasMaxLength(64).IsRequired();
            e.HasMany(x => x.Stops).WithOne().HasForeignKey(x => x.ItineraryId).OnDelete(DeleteBehavior.Cascade);
        });
        model.Entity<ItineraryStop>(e =>
        {
            e.HasKey(x => new { x.ItineraryId, x.ExhibitionId });
            e.Property(x => x.ExhibitionId).HasMaxLength(100);
            e.HasIndex(x => new { x.ItineraryId, x.Position }).IsUnique();
        });
    }
}
public class Itinerary
{
    public Guid Id { get; set; }
    public string AccessTokenHash { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public List<ItineraryStop> Stops { get; set; } = [];
}
public class ItineraryStop
{
    public Guid ItineraryId { get; set; }
    public string ExhibitionId { get; set; } = "";
    public int Position { get; set; }
}
