using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using MuseumExplorer.Api.Integrations.Sanity;
using Xunit;

public class SanityCatalogueTests
{
    private static object Document(string id = "ocean", string slug = "a-world-beneath") => new {
        _id = id, slug = new { current = slug }, title = "A world beneath", category = "Natural world",
        summary = "Ocean stories", description = "Explore the ocean.", gallery = "Gallery 01",
        minutes = 25, colour = "ocean", access = new[] { "Step-free access" }
    };
    private static HttpResponseMessage Json(object body, HttpStatusCode status = HttpStatusCode.OK) =>
        new(status) { Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json") };
    private static SanityExhibitionCatalogue Catalogue(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respond, string? token = null) =>
        new(new HttpClient(new StubHandler(respond)) { Timeout = TimeSpan.FromSeconds(1) },
            Options.Create(new SanityOptions { ProjectId = "test123", Dataset = "production", ReadToken = token }));

    [Fact] public async Task SendsGraphQlWithPublishedPerspectiveAndMapsStableId()
    {
        var catalogue = Catalogue(async (request, ct) => {
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal("https://test123.api.sanity.io/v2025-02-19/graphql/production/default?perspective=published", request.RequestUri!.ToString());
            Assert.Equal("Bearer", request.Headers.Authorization!.Scheme);
            Assert.Equal("test-secret", request.Headers.Authorization.Parameter);
            var body = await request.Content!.ReadAsStringAsync(ct);
            Assert.Contains("allExhibition", body);
            return Json(new { data = new { allExhibition = new[] { Document() } } });
        }, "test-secret");
        var exhibition = Assert.Single(await catalogue.GetAllAsync(default));
        Assert.Equal("ocean", exhibition.Id);
        Assert.Equal("a-world-beneath", exhibition.Slug);
        Assert.Equal(25, exhibition.Minutes);
    }

    [Fact] public async Task ExcludesDraftsAndReleaseVersionsEvenIfUpstreamReturnsThem()
    {
        var catalogue = Catalogue((_, _) => Task.FromResult(Json(new { data = new {
            allExhibition = new[] { Document(), Document("drafts.ocean"), Document("versions.release.ocean") }
        } })));
        Assert.Equal("ocean", Assert.Single(await catalogue.GetAllAsync(default)).Id);
    }

    [Fact] public async Task RejectsGraphQlErrorsEvenWithHttp200AndPartialData()
    {
        var catalogue = Catalogue((_, _) => Task.FromResult(Json(new {
            errors = new[] { new { message = "Sensitive upstream details" } },
            data = new { allExhibition = new[] { Document() } }
        })));
        var error = await Assert.ThrowsAsync<ContentUnavailableException>(() => catalogue.GetAllAsync(default));
        Assert.DoesNotContain("Sensitive", error.Message);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task UpstreamHttpFailureDoesNotReturnSampleContent(HttpStatusCode status)
    {
        var catalogue = Catalogue((_, _) => Task.FromResult(Json(new { message = "failure" }, status)));
        await Assert.ThrowsAsync<ContentUnavailableException>(() => catalogue.GetAllAsync(default));
    }

    [Fact] public async Task PaginatesWithoutDroppingExhibitions()
    {
        var calls = 0;
        var catalogue = Catalogue(async (request, ct) => {
            using var body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(ct));
            Assert.Equal(calls * 100, body.RootElement.GetProperty("variables").GetProperty("offset").GetInt32());
            var docs = calls++ == 0
                ? Enumerable.Range(0, 100).Select(i => Document($"id{i}", $"slug{i}")).ToArray()
                : new[] { Document("last", "last") };
            return Json(new { data = new { allExhibition = docs } });
        });
        Assert.Equal(101, (await catalogue.GetAllAsync(default)).Count);
        Assert.Equal(2, calls);
    }

    [Fact] public async Task RejectsMalformedPublishedContent()
    {
        var catalogue = Catalogue((_, _) => Task.FromResult(Json(new { data = new { allExhibition = new[] { new { _id = "broken" } } } })));
        await Assert.ThrowsAsync<ContentUnavailableException>(() => catalogue.GetAllAsync(default));
    }

    [Fact] public async Task EmptyPublishedDatasetIsAValidEmptyCatalogue()
    {
        var catalogue = Catalogue((_, _) => Task.FromResult(Json(new { data = new { allExhibition = Array.Empty<object>() } })));
        Assert.Empty(await catalogue.GetAllAsync(default));
    }

    [Fact] public async Task RequestCancellationIsPreserved()
    {
        using var source = new CancellationTokenSource();
        source.Cancel();
        var catalogue = Catalogue((_, ct) => Task.FromCanceled<HttpResponseMessage>(ct));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => catalogue.GetAllAsync(source.Token));
    }

    [Fact] public async Task TimeoutBecomesContentUnavailable()
    {
        var catalogue = Catalogue((_, _) => throw new TaskCanceledException());
        await Assert.ThrowsAsync<ContentUnavailableException>(() => catalogue.GetAllAsync(default));
    }

    [Fact] public async Task DuplicateSlugsAreRejected()
    {
        var catalogue = Catalogue((_, _) => Task.FromResult(Json(new { data = new {
            allExhibition = new[] { Document(), Document("other") }
        } })));
        await Assert.ThrowsAsync<ContentUnavailableException>(() => catalogue.GetAllAsync(default));
    }

    [Theory]
    [InlineData("https://example.com", "production")]
    [InlineData("test123", "../private")]
    public void RejectsInvalidEndpointConfiguration(string project, string dataset)
    {
        Assert.False(new SanityOptions { ProjectId = project, Dataset = dataset }.IsValid());
    }

    private sealed class StubHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            respond(request, cancellationToken);
    }
}
