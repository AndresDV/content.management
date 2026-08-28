using System.Net;
using System.Net.Http.Json;
using Content.Management.Application.Core.ContentManagementEntity.Queries.DTOs;
using FluentAssertions;
using Xunit;

namespace Content.Management.IntegrationTests;

[Collection(ContentManagementApiCollection.Name)]
public class ContentManagementVisibilityTests
{
    private readonly ContentManagementApiFixture _fixture;

    public ContentManagementVisibilityTests(ContentManagementApiFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task UnpublishedEntity_VisibleToAdmin_NotToUser()
    {
        var org = _fixture.OrganizationClient;
        var user = _fixture.UserClient;
        var admin = _fixture.AdminClient;

        await PublishAsync(org, "vis-1", version: 1);
        await UnpublishAsync(org, "vis-1", version: 1);

        var userGet = await user.GetAsync("/api/content-management/entities/vis-1");
        userGet.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var adminGet = await admin.GetAsync("/api/content-management/entities/vis-1");
        adminGet.StatusCode.Should().Be(HttpStatusCode.OK);

        var dto = await adminGet.Content.ReadFromJsonAsync<ContentManagementEntityDto>();
        dto!.IsPublished.Should().BeFalse();
    }

    [Fact]
    public async Task AdminDisabledEntity_HiddenFromUser_VisibleToAdmin()
    {
        var org = _fixture.OrganizationClient;
        var user = _fixture.UserClient;
        var admin = _fixture.AdminClient;

        await PublishAsync(org, "vis-2", version: 1);

        var disableResponse = await admin.PostAsync("/api/content-management/entities/vis-2/disable", null);
        disableResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var userGet = await user.GetAsync("/api/content-management/entities/vis-2");
        userGet.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var adminGet = await admin.GetAsync("/api/content-management/entities/vis-2");
        adminGet.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task List_UserSeesPublishedOnly_AdminSeesAll()
    {
        var org = _fixture.OrganizationClient;
        var user = _fixture.UserClient;
        var admin = _fixture.AdminClient;

        await PublishAsync(org, "vis-3", version: 1);
        await UnpublishAsync(org, "vis-4", version: 2);

        var userList = await (await user.GetAsync("/api/content-management/entities"))
            .Content.ReadFromJsonAsync<List<ContentManagementEntityDto>>();
        var adminList = await (await admin.GetAsync("/api/content-management/entities"))
            .Content.ReadFromJsonAsync<List<ContentManagementEntityDto>>();

        userList!.Any(x => x.Id == "vis-3").Should().BeTrue();
        userList!.Any(x => x.Id == "vis-4").Should().BeFalse();

        adminList!.Any(x => x.Id == "vis-3").Should().BeTrue();
        adminList!.Any(x => x.Id == "vis-4").Should().BeTrue();
    }

    private static Task<HttpResponseMessage> PublishAsync(HttpClient client, string id, int version) =>
        client.PostAsJsonAsync("/api/content-management/events", new[]
        {
            new { type = "publish", id, payload = new { v = version }, version }
        });

    private static Task<HttpResponseMessage> UnpublishAsync(HttpClient client, string id, int version) =>
        client.PostAsJsonAsync("/api/content-management/events", new[]
        {
            new { type = "unpublish", id, payload = new { v = version }, version }
        });
}
