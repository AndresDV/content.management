using Xunit;

namespace Content.Management.IntegrationTests;

/// <summary>Shares a single API/PostgreSQL fixture across integration test classes.</summary>
[CollectionDefinition(Name)]
public sealed class ContentManagementApiCollection : ICollectionFixture<ContentManagementApiFixture>
{
    public const string Name = "ContentManagementApi";
}
