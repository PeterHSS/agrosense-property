using Api.Domain.Entities;
using Api.Features.Plot;
using Api.Features.Plot.Update;
using Api.Infrastructure.Persistence.Contexts;
using Api.Infrastructure.Providers;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Test.Features.Plot;

public class UpdatePlotUseCaseTests
{
    private readonly AgroSenseDbContext _context;
    private readonly Mock<ICurrentUserProvider> _currentUserMock;
    private readonly UpdatePlotUseCase _useCase;

    public UpdatePlotUseCaseTests()
    {
        var options = new DbContextOptionsBuilder<AgroSenseDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AgroSenseDbContext(options);
        _currentUserMock = new Mock<ICurrentUserProvider>();

        _useCase = new UpdatePlotUseCase(_context, _currentUserMock.Object);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────────

    private void SetupCurrentUser(Guid userId) =>
        _currentUserMock.Setup(u => u.UserId).Returns(userId);

    private static UpdatePlotRequest BuildRequest(
        Guid plotId,
        string? name = "Talhão Atualizado",
        string? crop = "Milho",
        decimal area = 200m) => new(plotId, name, crop, area);

    private async Task<(Producer producer, Api.Domain.Entities.Property property, Api.Domain.Entities.Plot plot)> SeedFullChainAsync(Guid userId)
    {
        var producer = new Producer { Id = Guid.NewGuid(), UserId = userId };
        _context.Producers.Add(producer);

        var property = new Api.Domain.Entities.Property { Id = Guid.NewGuid(), ProducerId = producer.Id, Producer = producer };
        _context.Properties.Add(property);

        var plot = new Api.Domain.Entities.Plot
        {
            Id = Guid.NewGuid(),
            Name = "Talhão Original",
            Crop = "Soja",
            Area = 100m,
            PropertyId = property.Id,
            Property = property,
            CreatedAt = DateTime.UtcNow
        };
        _context.Plots.Add(plot);

        await _context.SaveChangesAsync();

        return (producer, property, plot);
    }

    private async Task<Api.Domain.Entities.Plot> SeedOrphanPlotAsync()
    {
        // Plot with no producer linked to current user
        var otherProducer = new Producer { Id = Guid.NewGuid(), UserId = Guid.NewGuid() };
        _context.Producers.Add(otherProducer);

        var property = new Api.Domain.Entities.Property { Id = Guid.NewGuid(), ProducerId = otherProducer.Id, Producer = otherProducer };
        _context.Properties.Add(property);

        var plot = new Api.Domain.Entities.Plot
        {
            Id = Guid.NewGuid(),
            Name = "Talhão Alheio",
            Crop = "Trigo",
            Area = 80m,
            PropertyId = property.Id,
            Property = property,
            CreatedAt = DateTime.UtcNow
        };
        _context.Plots.Add(plot);

        await _context.SaveChangesAsync();

        return plot;
    }

    // ─── Plot not found ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenPlotNotFound_ReturnsPlotNotFound()
    {
        // Arrange
        SetupCurrentUser(Guid.NewGuid());
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _useCase.Handle(BuildRequest(nonExistentId));

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(PlotErrors.NotFound(nonExistentId).Code, result.Error.Code);
    }

    [Fact]
    public async Task Handle_WhenPlotNotFound_DoesNotAlterDatabase()
    {
        // Arrange
        SetupCurrentUser(Guid.NewGuid());

        // Act
        await _useCase.Handle(BuildRequest(Guid.NewGuid()));

        // Assert
        Assert.Empty(_context.Plots);
    }

    // ─── Not owner ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenUserIsNotOwner_ReturnsForbiddenOperation()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupCurrentUser(userId); // current user is NOT the owner
        var plot = await SeedOrphanPlotAsync();

        // Act
        var result = await _useCase.Handle(BuildRequest(plot.Id));

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(PlotErrors.ForbbidenOperation.Code, result.Error.Code);
    }

    [Fact]
    public async Task Handle_WhenUserIsNotOwner_DoesNotModifyPlot()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupCurrentUser(userId);
        var plot = await SeedOrphanPlotAsync();
        var originalName = plot.Name;

        // Act
        await _useCase.Handle(BuildRequest(plot.Id, name: "Tentativa de alteração"));

        // Assert
        var unchanged = await _context.Plots.FindAsync(plot.Id);
        Assert.Equal(originalName, unchanged!.Name);
    }

    // ─── Partial updates ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenNameIsNull_KeepsOriginalName()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupCurrentUser(userId);
        var (_, _, plot) = await SeedFullChainAsync(userId);
        var originalName = plot.Name;

        // Act
        await _useCase.Handle(BuildRequest(plot.Id, name: null));

        // Assert
        var updated = await _context.Plots.FindAsync(plot.Id);
        Assert.Equal(originalName, updated!.Name);
    }

    [Fact]
    public async Task Handle_WhenCropIsNull_KeepsOriginalCrop()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupCurrentUser(userId);
        var (_, _, plot) = await SeedFullChainAsync(userId);
        var originalCrop = plot.Crop;

        // Act
        await _useCase.Handle(BuildRequest(plot.Id, crop: null));

        // Assert
        var updated = await _context.Plots.FindAsync(plot.Id);
        Assert.Equal(originalCrop, updated!.Crop);
    }

    [Fact]
    public async Task Handle_WhenAreaIsDefault_KeepsOriginalArea()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupCurrentUser(userId);
        var (_, _, plot) = await SeedFullChainAsync(userId);
        var originalArea = plot.Area;

        // Act
        await _useCase.Handle(BuildRequest(plot.Id, area: default));

        // Assert
        var updated = await _context.Plots.FindAsync(plot.Id);
        Assert.Equal(originalArea, updated!.Area);
    }

    // ─── Happy path ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenEverythingValid_ReturnsSuccess()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupCurrentUser(userId);
        var (_, _, plot) = await SeedFullChainAsync(userId);

        // Act
        var result = await _useCase.Handle(BuildRequest(plot.Id));

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_WhenEverythingValid_UpdatesAllProvidedFields()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupCurrentUser(userId);
        var (_, _, plot) = await SeedFullChainAsync(userId);

        var request = BuildRequest(plot.Id, name: "Novo Nome", crop: "Café", area: 350m);

        // Act
        await _useCase.Handle(request);

        // Assert
        var updated = await _context.Plots.FindAsync(plot.Id);
        Assert.Equal("Novo Nome", updated!.Name);
        Assert.Equal("Café", updated.Crop);
        Assert.Equal(350m, updated.Area);
    }

    [Fact]
    public async Task Handle_WhenEverythingValid_DoesNotAffectOtherPlots()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupCurrentUser(userId);
        var (_, property, plot) = await SeedFullChainAsync(userId);

        var otherPlot = new Api.Domain.Entities.Plot
        {
            Id = Guid.NewGuid(),
            Name = "Outro Talhão",
            Crop = "Arroz",
            Area = 60m,
            PropertyId = property.Id,
            Property = property,
            CreatedAt = DateTime.UtcNow
        };
        _context.Plots.Add(otherPlot);
        await _context.SaveChangesAsync();

        // Act
        await _useCase.Handle(BuildRequest(plot.Id, name: "Alterado"));

        // Assert
        var untouched = await _context.Plots.FindAsync(otherPlot.Id);
        Assert.Equal("Outro Talhão", untouched!.Name);
    }
}