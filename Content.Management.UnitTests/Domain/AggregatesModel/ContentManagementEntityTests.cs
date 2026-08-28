using Content.Management.Domain;
using Content.Management.Domain.AggregatesModel.ContentManagementEntityAggregate;
using FluentAssertions;
using Xunit;

namespace Content.Management.UnitTests.Domain.AggregatesModel;

public class ContentManagementEntityTests
{
    [Fact]
    public void Create_WhenPublished_IsPublishedAtVersion()
    {
        var entity = new ContentManagementEntity("id-1", "{\"name\":\"first\"}", 1, isPublished: true);

        entity.Version.Should().Be(1);
        entity.IsPublished.Should().BeTrue();
    }

    [Fact]
    public void Publish_AdvancesVersionAndMarksPublished()
    {
        var entity = new ContentManagementEntity("id-1", "v1", 1, true);

        var applied = entity.Publish(2, "v2");

        applied.Should().BeTrue();
        entity.Version.Should().Be(2);
        entity.Payload.Should().Be("v2");
        entity.IsPublished.Should().BeTrue();
    }

    [Fact]
    public void Publish_StaleVersion_IsIgnored()
    {
        var entity = new ContentManagementEntity("id-1", "v2", 2, true);

        var applied = entity.Publish(1, "v1");

        applied.Should().BeFalse();
        entity.Version.Should().Be(2);
        entity.Payload.Should().Be("v2");
    }

    [Fact]
    public void Unpublish_MarksUnpublishedAndRetainsVersion()
    {
        var entity = new ContentManagementEntity("id-1", "v1", 1, true);

        var applied = entity.Unpublish(1, "v1");

        applied.Should().BeTrue();
        entity.IsPublished.Should().BeFalse();
        entity.Version.Should().Be(1);
    }

    [Fact]
    public void Unpublish_StaleVersion_IsIgnored()
    {
        var entity = new ContentManagementEntity("id-1", "v3", 3, true);

        var applied = entity.Unpublish(2, "v2");

        applied.Should().BeFalse();
        entity.IsPublished.Should().BeTrue();
        entity.Version.Should().Be(3);
    }

    [Fact]
    public void IsVisibleTo_User_RequiresPublishedAndNotDisabled()
    {
        new ContentManagementEntity("published", "v1", 1, true)
            .IsVisibleTo(UserRole.User).Should().BeTrue();

        new ContentManagementEntity("unpublished", "v1", 1, false)
            .IsVisibleTo(UserRole.User).Should().BeFalse();

        var disabled = new ContentManagementEntity("disabled", "v1", 1, true);
        disabled.Disable("admin@example.com");
        disabled.IsVisibleTo(UserRole.User).Should().BeFalse();
    }

    [Fact]
    public void IsVisibleTo_Admin_AlwaysTrue()
    {
        new ContentManagementEntity("unpublished", "v1", 1, false)
            .IsVisibleTo(UserRole.Admin).Should().BeTrue();

        var disabled = new ContentManagementEntity("disabled", "v1", 1, true);
        disabled.Disable("admin@example.com");
        disabled.IsVisibleTo(UserRole.Admin).Should().BeTrue();
    }

    [Fact]
    public void Disable_RecordsAdminAndTimestamp()
    {
        var entity = new ContentManagementEntity("id-1", "v1", 1, true);

        entity.Disable("admin@example.com");

        entity.IsDisabled.Should().BeTrue();
        entity.DisabledBy.Should().Be("admin@example.com");
        entity.DisabledAt.Should().NotBeNull();
    }
}
