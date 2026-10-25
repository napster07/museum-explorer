namespace MuseumExplorer.Api.Features;

public interface IExhibitionCatalogue
{
    Task<IReadOnlyList<Exhibition>> GetAllAsync(CancellationToken cancellationToken);
}

public sealed class SampleExhibitionCatalogue : IExhibitionCatalogue
{
    public Task<IReadOnlyList<Exhibition>> GetAllAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Exhibition>>(Exhibitions.All);
}
