using System.Text.Json;
using Content.Management.Application.Core.ContentManagementEntity.Commands;
using Content.Management.Application.Core.ContentManagementEntity.Events;
using Content.Management.Application.Core.ContentManagementEntity.Validations;
using FluentAssertions;
using Xunit;

namespace Content.Management.UnitTests.Application.Validations;

public class ContentManagementEntityValidatorsTests
{
    [Fact]
    public void PublishEvent_WithPayloadAndVersion_IsValid()
    {
        var validator = new ContentEventRequestValidator();

        var result = validator.Validate(PublishEvent(version: 2));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void PublishEvent_WithoutVersion_IsInvalid()
    {
        var validator = new ContentEventRequestValidator();

        var result = validator.Validate(new ContentEventRequest("publish", "id-1", Payload(), null, null));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void PublishEvent_WithoutPayload_IsInvalid()
    {
        var validator = new ContentEventRequestValidator();

        var result = validator.Validate(new ContentEventRequest("publish", "id-1", null, 1, null));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void DeleteEvent_WithoutPayloadOrVersion_IsValid()
    {
        var validator = new ContentEventRequestValidator();

        var result = validator.Validate(new ContentEventRequest("delete", "id-1", null, null, null));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void UnknownEventType_IsInvalid()
    {
        var validator = new ContentEventRequestValidator();

        var result = validator.Validate(new ContentEventRequest("rename", "id-1", Payload(), 1, null));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void PublishCommand_WithMissingPayload_IsInvalid()
    {
        var validator = new PublishContentManagementEntityValidator();

        var result = validator.Validate(new PublishContentManagementEntityCommand("id-1", "", 1));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void PublishCommand_WithNonPositiveVersion_IsInvalid()
    {
        var validator = new PublishContentManagementEntityValidator();

        var result = validator.Validate(new PublishContentManagementEntityCommand("id-1", "{}", 0));

        result.IsValid.Should().BeFalse();
    }

    private static ContentEventRequest PublishEvent(int version) =>
        new("publish", "id-1", Payload(), version, null);

    private static JsonElement Payload()
    {
        using var doc = JsonDocument.Parse("{\"name\":\"first\"}");
        return doc.RootElement.Clone();
    }
}
