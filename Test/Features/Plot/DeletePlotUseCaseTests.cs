using Api.Domain.Entities;
using Api.Features.Plot;
using Api.Features.Plot.Delete;
using Api.Features.Producer;
using Api.Infrastructure.Persistence.Contexts;
using Api.Infrastructure.Providers;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Test.Features.Plot;

public class DeletePlotUseCaseTests
{
    private readonly AgroSenseDbContext _context;
    private readonly Mock<ICurrentUserProvider> _currentUserMock;
    private readonly DeletePlotUseCase _useCase;

    public DeletePlotUseCaseTests()
    {
        var options = new DbContextOptionsBuilder<AgroSenseDbContext>().UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()).Options;
        _context = new AgroSenseDbContext(options);
        _currentUserMock = new Mock<ICurrentUserProvider>();
        _useCase = new DeletePlotUseCase(_context, _currentUserMock.Object);
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────────

    private void SetupCurrentUser(Guid userId) 
        =>_currentUserMock.Setup(u => u.UserId).Returns(userId);

    private static DeletePlotRequest BuildRequest(Guid plotId) 
        => new(plotId);

    private async Task<Producer> SeedProducerAsync(Guid userId)
    {
        var producer = new Producer { Id = Guid.NewGuid(), UserId = userId };
        _context.Producers.Add(producer);
        await _context.SaveChangesAsync();
        return producer;
    }

    private async Task<Api.Domain.Entities.Property> SeedPropertyAsync(Guid producerId)
    {
        var property = new Api.Domain.Entities.Property { Id = Guid.NewGuid(), ProducerId = producerId };
        _context.Properties.Add(property);
        await _context.SaveChangesAsync();
        return property;
    }

    private async Task<Api.Domain.Entities.Plot> SeedPlotAsync(Guid propertyId)
    {
        var plot = new Api.Domain.Entities.Plot
        {
            Id = Guid.NewGuid(),
            Name = "Talhão Norte",
            Crop = "Soja",
            Area = 50m,
            PropertyId = propertyId,
            CreatedAt = DateTime.UtcNow
        };
        _context.Plots.Add(plot);
        await _context.SaveChangesAsync();
        return plot;
    }

    // ─── Producer not found ───────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenProducerNotFound_ReturnsProducerNotFound()
    {
        // Arrange
        SetupCurrentUser(Guid.NewGuid()); // No producer seeded

        // Act
        var result = await _useCase.Handle(BuildRequest(Guid.NewGuid()));

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ProducerErrors.NotFound.Code, result.Error.Code);
    }

    [Fact]
    public async Task Handle_WhenProducerNotFound_DoesNotAlterDatabase()
    {
        // Arrange
        SetupCurrentUser(Guid.NewGuid());

        // Act
        await _useCase.Handle(BuildRequest(Guid.NewGuid()));

        // Assert
        Assert.Empty(_context.Plots);
    }

    // ─── Plot not found ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenPlotNotFound_ReturnsPlotNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupCurrentUser(userId);
        await SeedProducerAsync(userId);

        var nonExistentPlotId = Guid.NewGuid();

        // Act
        var result = await _useCase.Handle(BuildRequest(nonExistentPlotId));

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(PlotErrors.NotFound(nonExistentPlotId).Code, result.Error.Code);
    }

    // ─── Plot belongs to another producer ────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenPlotBelongsToOtherProducer_ReturnsPlotDoesntBelongToProducer()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupCurrentUser(userId);
        await SeedProducerAsync(userId); // current producer (not the owner)

        var anotherProducerId = Guid.NewGuid();
        var property = await SeedPropertyAsync(anotherProducerId);
        var plot = await SeedPlotAsync(property.Id);

        // Act
        var result = await _useCase.Handle(BuildRequest(plot.Id));

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(PlotErrors.PlotDoesntBelongToProducer.Code, result.Error.Code);
    }

    [Fact]
    public async Task Handle_WhenPlotBelongsToOtherProducer_DoesNotDeletePlot()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupCurrentUser(userId);
        await SeedProducerAsync(userId);

        var anotherProducerId = Guid.NewGuid();
        var property = await SeedPropertyAsync(anotherProducerId);
        var plot = await SeedPlotAsync(property.Id);

        // Act
        await _useCase.Handle(BuildRequest(plot.Id));

        // Assert
        Assert.NotNull(await _context.Plots.FindAsync(plot.Id));
    }

    // ─── Happy path ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenEverythingValid_ReturnsSuccess()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupCurrentUser(userId);
        var producer = await SeedProducerAsync(userId);
        var property = await SeedPropertyAsync(producer.Id);
        var plot = await SeedPlotAsync(property.Id);

        // Act
        var result = await _useCase.Handle(BuildRequest(plot.Id));

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_WhenEverythingValid_RemovesPlotFromDatabase()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupCurrentUser(userId);
        var producer = await SeedProducerAsync(userId);
        var property = await SeedPropertyAsync(producer.Id);
        var plot = await SeedPlotAsync(property.Id);

        // Act
        await _useCase.Handle(BuildRequest(plot.Id));

        // Assert
        Assert.Null(await _context.Plots.FindAsync(plot.Id));
    }

    [Fact]
    public async Task Handle_WhenEverythingValid_DoesNotRemoveOtherPlots()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupCurrentUser(userId);
        var producer = await SeedProducerAsync(userId);
        var property = await SeedPropertyAsync(producer.Id);

        var plotToDelete = await SeedPlotAsync(property.Id);
        var otherPlot = await SeedPlotAsync(property.Id);

        // Act
        await _useCase.Handle(BuildRequest(plotToDelete.Id));

        // Assert
        Assert.NotNull(await _context.Plots.FindAsync(otherPlot.Id));
        Assert.Single(_context.Plots);
    }
}