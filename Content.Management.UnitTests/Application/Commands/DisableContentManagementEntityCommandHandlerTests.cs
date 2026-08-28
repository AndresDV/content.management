using Content.Management.Application.Core.ContentManagementEntity.Commands;
using Content.Management.Application.Core.ContentManagementEntity.Validations;
using Content.Management.Domain.AggregatesModel.ContentManagementEntityAggregate;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Content.Management.UnitTests.Application.Commands;

public class DisableContentManagementEntityCommandHandlerTests
{
    private readonly IContentManagementEntityRepository _repository = Substitute.For<IContentManagementEntityRepository>();
    private readonly DisableContentManagementEntityValidator _validator = new();

    private DisableContentManagementEntityCommandHandler CreateHandler() =>
        new(NullLogger<DisableContentManagementEntityCommandHandler>.Instance, _repository, _validator);

    [Fact]
    public async Task Handle_WhenEntityNotFound_ReturnsFalse()
    {
        _repository.FindAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((ContentManagementEntity?)null);

        var result = await CreateHandler().Handle(
            new DisableContentManagementEntityCommand("missing", "admin@example.com"),
            CancellationToken.None);

        result.Should().BeFalse();
        _repository.DidNotReceive().Update(Arg.Any<ContentManagementEntity>());
    }

    [Fact]
    public async Task Handle_WhenEntityFound_DisablesAndReturnsTrue()
    {
        var entity = new ContentManagementEntity("id-1", "v1", 1, true);
        _repository.FindAsync("id-1", Arg.Any<CancellationToken>()).Returns(entity);
        _repository.UnitOfWork.SaveEntitiesAsync(Arg.Any<CancellationToken>()).Returns(true);

        var result = await CreateHandler().Handle(
            new DisableContentManagementEntityCommand("id-1", "admin@example.com"),
            CancellationToken.None);

        result.Should().BeTrue();
        entity.IsDisabled.Should().BeTrue();
        entity.DisabledBy.Should().Be("admin@example.com");
        entity.DisabledAt.Should().NotBeNull();
        _repository.Received(1).Update(entity);
    }
}
