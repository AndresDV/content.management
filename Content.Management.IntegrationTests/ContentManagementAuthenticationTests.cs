using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace Content.Management.IntegrationTests;

[Collection(ContentManagementApiCollection.Name)]
public class ContentManagementAuthenticationTests
{
    private readonly ContentManagementApiFixture _fixture;

    public ContentManagementAuthenticationTests(ContentManagementApiFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Webhook_WithoutAuth_Returns401()
    {
        var response = await _fixture.CreateClient().PostAsJsonAsync(
            "/api/content-management/events",
            new[] { new { type = "publish", id = "auth-1", payload = new { a = 1 }, version = 1 } });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Webhook_WithInvalidPassword_Returns401()
    {
        var client = _fixture.CreateClient(ContentManagementApiFixture.OrganizationUsername, "wrong-password");

        var response = await client.PostAsJsonAsync(
            "/api/content-management/events",
            new[] { new { type = "publish", id = "auth-2", payload = new { a = 1 }, version = 1 } });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Read_WithoutAuth_Returns401()
    {
        var response = await _fixture.CreateClient().GetAsync("/api/content-management/entities");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Read_WithInvalidPassword_Returns401()
    {
        var client = _fixture.CreateClient(ContentManagementApiFixture.UserUsername, "wrong-password");

        var response = await client.GetAsync("/api/content-management/entities");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Disable_AsNonAdmin_Returns403()
    {
        var response = await _fixture.UserClient.PostAsync("/api/content-management/entities/auth-3/disable", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
