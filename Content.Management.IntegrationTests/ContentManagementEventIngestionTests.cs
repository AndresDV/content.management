using System.Net;
using System.Net.Http.Json;
using System.Text;
using Content.Management.Application.Core.ContentManagementEntity.Queries.DTOs;
using FluentAssertions;
using Xunit;

namespace Content.Management.IntegrationTests;

[Collection(ContentManagementApiCollection.Name)]
public class ContentManagementEventIngestionTests
{
    private readonly ContentManagementApiFixture _fixture;

    public ContentManagementEventIngestionTests(ContentManagementApiFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task PublishThenQuery_RoundTrip()
    {
        var org = _fixture.OrganizationClient;
        var user = _fixture.UserClient;

        var publishResponse = await PublishAsync(org, "ingest-1", new { name = "first" }, version: 1);
        publishResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var getResponse = await user.GetAsync("/api/content-management/entities/ingest-1");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var dto = await getResponse.Content.ReadFromJsonAsync<ContentManagementEntityDto>();
        dto!.Version.Should().Be(1);
        dto.IsPublished.Should().BeTrue();
    }

    [Fact]
    public async Task Unpublish_HidesEntityFromReadApi()
    {
        var org = _fixture.OrganizationClient;
        var user = _fixture.UserClient;

        await PublishAsync(org, "ingest-2", new { a = 1 }, version: 1);

        var unpublishResponse = await org.PostAsJsonAsync("/api/content-management/events", new[]
        {
            new { type = "unpublish", id = "ingest-2", payload = new { a = 1 }, version = 1 }
        });
        unpublishResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var getResponse = await user.GetAsync("/api/content-management/entities/ingest-2");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UnpublishNeverPublished_CornerCase_EntityIsNotVisible()
    {
        var org = _fixture.OrganizationClient;
        var user = _fixture.UserClient;

        var response = await org.PostAsJsonAsync("/api/content-management/events", new[]
        {
            new { type = "unpublish", id = "ingest-3", payload = new { a = 1 }, version = 2 }
        });
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var getResponse = await user.GetAsync("/api/content-management/entities/ingest-3");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_RemovesEntity()
    {
        var org = _fixture.OrganizationClient;
        var user = _fixture.UserClient;

        await PublishAsync(org, "ingest-4", new { a = 1 }, version: 1);

        await org.PostAsJsonAsync("/api/content-management/events", new[]
        {
            new { type = "delete", id = "ingest-4" }
        });

        var getResponse = await user.GetAsync("/api/content-management/entities/ingest-4");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task OutOfOrderPublish_KeepsHighestVersion()
    {
        var org = _fixture.OrganizationClient;
        var user = _fixture.UserClient;

        await PublishAsync(org, "ingest-5", new { v = 3 }, version: 3);
        await PublishAsync(org, "ingest-5", new { v = 2 }, version: 2);

        var getResponse = await user.GetAsync("/api/content-management/entities/ingest-5");
        var dto = await getResponse.Content.ReadFromJsonAsync<ContentManagementEntityDto>();

        dto!.Version.Should().Be(3);
    }

    [Fact]
    public async Task InvalidEvent_ReturnsBadRequest()
    {
        var org = _fixture.OrganizationClient;

        var response = await org.PostAsJsonAsync("/api/content-management/events", new[]
        {
            new { type = "publish", id = "ingest-6", payload = new { a = 1 } }
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Publish_PrettyPrintedPayload_StoresCompactJson()
    {
        var org = _fixture.OrganizationClient;
        var user = _fixture.UserClient;

        var body = """
        [
          {
            "type": "publish",
            "id": "ingest-7",
            "payload": {
              "name": "My Event",
              "nested": { "a": 1 }
            },
            "version": 1
          }
        ]
        """;

        var content = new StringContent(body, Encoding.UTF8, "application/json");
        var publishResponse = await org.PostAsync("/api/content-management/events", content);
        publishResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var getResponse = await user.GetAsync("/api/content-management/entities/ingest-7");
        var dto = await getResponse.Content.ReadFromJsonAsync<ContentManagementEntityDto>();

        dto!.Payload.Should().Be("{\"name\":\"My Event\",\"nested\":{\"a\":1}}");
    }

    private static Task<HttpResponseMessage> PublishAsync(HttpClient client, string id, object payload, int version) =>
        client.PostAsJsonAsync("/api/content-management/events", new[]
        {
            new { type = "publish", id, payload, version, timestamp = "2024-01-01T00:00:00Z" }
        });
}
