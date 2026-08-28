using Content.Management.Application.Core.ContentManagementEntity.Commands;
using Content.Management.Application.Core.ContentManagementEntity.Validations;
using Content.Management.Domain.AggregatesModel.ContentManagementEntityAggregate;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Content.Management.UnitTests.Application.Commands;

public class DeleteContentManagementEntityCommandHandlerTests
{
    private readonly IContentManagementEntityRepository _repository = Substitute.For<IContentManagementEntityRepository>();
    private readonly DeleteContentManagementEntityValidator _validator = new();

    private DeleteContentManagementEntityCommandHandler CreateHandler() =>
        new(NullLogger<DeleteContentManagementEntityCommandHandler>.Instance, _repository, _validator);

    [Fact]
    public async Task Handle_WhenEntityNotFound_ReturnsTrueWithoutDeleting()
    {
        _repository.FindAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((ContentManagementEntity?)null);

        var result = await CreateHandler().Handle(
            new DeleteContentManagementEntityCommand("missing"),
            CancellationToken.None);

        result.Should().BeTrue();
        _repository.DidNotReceive().Delete(Arg.Any<ContentManagementEntity>());
    }

    [Fact]
    public async Task Handle_WhenEntityFound_DeletesAndReturnsTrue()
    {
        var entity = new ContentManagementEntity("id-1", "v1", 1, true);
        _repository.FindAsync("id-1", Arg.Any<CancellationToken>()).Returns(entity);
        _repository.UnitOfWork.SaveEntitiesAsync(Arg.Any<CancellationToken>()).Returns(true);

        var result = await CreateHandler().Handle(
            new DeleteContentManagementEntityCommand("id-1"),
            CancellationToken.None);

        result.Should().BeTrue();
        _repository.Received(1).Delete(entity);
    }
}
