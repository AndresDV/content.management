using Content.Management.Application.Core.ContentManagementEntity.Commands;
using Content.Management.Application.Core.ContentManagementEntity.Validations;
using Content.Management.Domain.AggregatesModel.ContentManagementEntityAggregate;
using FluentAssertions;
using FluentValidation;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Content.Management.UnitTests.Application.Commands;

public class PublishContentManagementEntityCommandHandlerTests
{
    private readonly IContentManagementEntityRepository _repository = Substitute.For<IContentManagementEntityRepository>();
    private readonly PublishContentManagementEntityValidator _validator = new();

    private PublishContentManagementEntityCommandHandler CreateHandler() =>
        new(NullLogger<PublishContentManagementEntityCommandHandler>.Instance, _repository, _validator);

    [Fact]
    public async Task Handle_WhenEntityDoesNotExist_CreatesPublishedEntity()
    {
        _repository.FindAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((ContentManagementEntity?)null);
        _repository.UnitOfWork.SaveEntitiesAsync(Arg.Any<CancellationToken>()).Returns(true);

        var result = await CreateHandler().Handle(
            new PublishContentManagementEntityCommand("id-1", "{\"name\":\"first\"}", 1),
            CancellationToken.None);

        result.Should().BeTrue();
        await _repository.Received(1).AddAsync(
            Arg.Is<ContentManagementEntity>(e => e.IsPublished && e.Version == 1),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenEntityExists_PublishesAndReturnsTrue()
    {
        var entity = new ContentManagementEntity("id-1", "v1", 1, true);
        _repository.FindAsync("id-1", Arg.Any<CancellationToken>()).Returns(entity);
        _repository.UnitOfWork.SaveEntitiesAsync(Arg.Any<CancellationToken>()).Returns(true);

        var result = await CreateHandler().Handle(
            new PublishContentManagementEntityCommand("id-1", "v2", 2),
            CancellationToken.None);

        result.Should().BeTrue();
        entity.Version.Should().Be(2);
        _repository.Received(1).Update(entity);
    }

    [Fact]
    public async Task Handle_WhenStaleVersion_ReturnsTrueWithoutUpdating()
    {
        var entity = new ContentManagementEntity("id-1", "v3", 3, true);
        _repository.FindAsync("id-1", Arg.Any<CancellationToken>()).Returns(entity);

        var result = await CreateHandler().Handle(
            new PublishContentManagementEntityCommand("id-1", "v2", 2),
            CancellationToken.None);

        result.Should().BeTrue();
        entity.Version.Should().Be(3);
        _repository.DidNotReceive().Update(Arg.Any<ContentManagementEntity>());
    }

    [Fact]
    public async Task Handle_WhenCommandIsInvalid_ThrowsValidationException()
    {
        Func<Task> act = () => CreateHandler().Handle(
            new PublishContentManagementEntityCommand("", "", 0),
            CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }
}
