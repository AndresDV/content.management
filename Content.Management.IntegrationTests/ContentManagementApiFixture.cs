using System.Net.Http.Headers;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Testcontainers.PostgreSql;
using Xunit;

namespace Content.Management.IntegrationTests;

/// <summary>
/// Shared integration fixture: spins up a PostgreSQL container and hosts the API
/// in-process, exposing authenticated clients for the two Basic-auth pools.
/// </summary>
public sealed class ContentManagementApiFixture : IAsyncLifetime
{
    public const string OrganizationUsername = "content-cms-service";
    public const string OrganizationPassword = "a1b2c3d4-e5f6-4789-abcd-ef0123456789";
    public const string UserUsername = "user@example.com";
    public const string UserPassword = "user-password";
    public const string AdminUsername = "admin@example.com";
    public const string AdminPassword = "admin-password";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:18-alpine")
        .WithDatabase("contentmanagement")
        .WithUsername("content")
        .WithPassword("content")
        .Build();

    private WebApplicationFactory<Program> _factory = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:ContentManagement", _postgres.GetConnectionString());
            builder.UseEnvironment("Testing");

            // Tests are self-contained: inject the auth credentials directly so they
            // don't depend on committed appsettings files.
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Authentication:Organization:Username"] = OrganizationUsername,
                    ["Authentication:Organization:Password"] = OrganizationPassword,
                    ["Authentication:Users:ApiUsers:0:Username"] = UserUsername,
                    ["Authentication:Users:ApiUsers:0:Password"] = UserPassword,
                    ["Authentication:Users:ApiUsers:0:Role"] = "User",
                    ["Authentication:Users:ApiUsers:1:Username"] = AdminUsername,
                    ["Authentication:Users:ApiUsers:1:Password"] = AdminPassword,
                    ["Authentication:Users:ApiUsers:1:Role"] = "Admin"
                }));
        });
    }

    public async Task DisposeAsync()
    {
        await _factory.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    public HttpClient CreateClient() => _factory.CreateClient();

    public HttpClient CreateClient(string username, string password)
    {
        var client = _factory.CreateClient();
        var token = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", token);
        return client;
    }

    public HttpClient OrganizationClient => CreateClient(OrganizationUsername, OrganizationPassword);

    public HttpClient UserClient => CreateClient(UserUsername, UserPassword);

    public HttpClient AdminClient => CreateClient(AdminUsername, AdminPassword);
}
