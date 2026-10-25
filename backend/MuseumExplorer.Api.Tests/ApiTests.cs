using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using MuseumExplorer.Api.Features;
using Xunit;
using Microsoft.Extensions.Configuration;

public class ApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient client;
    public ApiTests(WebApplicationFactory<Program> factory)
    {
        client = factory.WithWebHostBuilder(builder => {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
                new Dictionary<string, string?> { ["Content:Provider"] = "Sample" }));
        }).CreateClient();
    }
    [Fact] public async Task CatalogueHasThreeDistinctExhibitions()
    {
        var exhibitions = await client.GetFromJsonAsync<Exhibition[]>("/api/exhibitions");
        Assert.Equal(3, exhibitions!.Length);
        Assert.Equal(3, exhibitions.Select(x => x.Id).Distinct().Count());
    }
    [Theory]
    [InlineData("""{"exhibitionIds":[]}""")]
    [InlineData("""{"exhibitionIds":["ocean","ocean"]}""")]
    [InlineData("""{"exhibitionIds":["missing"]}""")]
    [InlineData("""{"exhibitionIds":null}""")]
    public async Task InvalidItinerariesAreRejectedBeforeDatabaseAccess(string json)
    {
        var response = await client.PostAsync("/api/itineraries", new StringContent(json, System.Text.Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
    [Fact] public async Task RetrievalWithoutTokenIsUnauthorized()
    {
        var response = await client.GetAsync($"/api/itineraries/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
    [Fact] public async Task UnknownExhibitionIsNotFound()
    {
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/exhibitions/missing")).StatusCode);
    }
    [Fact] public void TokenVerificationRejectsOtherSecrets()
    {
        var token = new string('A', 64);
        Assert.True(ItineraryRules.Matches(token, ItineraryRules.Hash(token)));
        Assert.False(ItineraryRules.Matches(new string('B', 64), ItineraryRules.Hash(token)));
        Assert.False(ItineraryRules.Matches("", ItineraryRules.Hash(token)));
    }
}
