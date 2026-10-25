using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MuseumExplorer.Api.Features;
using MuseumExplorer.Api.Integrations.Sanity;
using Xunit;

public class ContentApiTests
{
    private static WebApplicationFactory<Program> Factory(IExhibitionCatalogue catalogue) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder => {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
                new Dictionary<string, string?> { ["Content:Provider"] = "Sample" }));
            builder.ConfigureServices(services => {
                services.RemoveAll<IExhibitionCatalogue>();
                services.AddSingleton(catalogue);
            });
        });

    [Fact] public async Task RestRoutesUseInjectedCatalogue()
    {
        var custom = Exhibitions.All[0] with { Id = "cms-id", Slug = "cms-slug", Title = "Published in Sanity" };
        await using var factory = Factory(new FixedCatalogue([custom]));
        using var client = factory.CreateClient();
        var response = await client.GetFromJsonAsync<Exhibition>("/api/exhibitions/cms-slug");
        Assert.Equal(custom, response! with { Access = custom.Access });
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/exhibitions/a-world-beneath")).StatusCode);
    }

    [Fact] public async Task ItineraryValidationUsesCmsInsteadOfOldSampleCatalogue()
    {
        await using var factory = Factory(new FixedCatalogue([]));
        using var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/itineraries", new { exhibitionIds = new[] { "ocean" } });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact] public async Task UpstreamFailureReturnsSanitized503()
    {
        await using var factory = Factory(new FailedCatalogue());
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/api/exhibitions");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.DoesNotContain("secret", await response.Content.ReadAsStringAsync());
    }

    private sealed class FixedCatalogue(IReadOnlyList<Exhibition> exhibitions) : IExhibitionCatalogue
    {
        public Task<IReadOnlyList<Exhibition>> GetAllAsync(CancellationToken ct) => Task.FromResult(exhibitions);
    }
    private sealed class FailedCatalogue : IExhibitionCatalogue
    {
        public Task<IReadOnlyList<Exhibition>> GetAllAsync(CancellationToken ct) =>
            throw new ContentUnavailableException("secret");
    }
}
