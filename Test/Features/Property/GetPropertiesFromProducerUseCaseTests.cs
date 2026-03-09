using Api.Domain.Abstractions.UseCases;
using Api.Domain.Entities;
using Api.Features.Producer;
using Api.Features.Property.GetPropertiesFromProducer;
using Api.Infrastructure.Persistence.Contexts;
using Api.Infrastructure.Providers;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Test.Features.Property;

public class GetPropertiesFromProducerUseCaseTests
{
    private readonly AgroSenseDbContext _context;
    private readonly Mock<ICurrentUserProvider> _currentUserMock;
    private readonly GetPropertiesFromProducerUseCase _useCase;

    public GetPropertiesFromProducerUseCaseTests()
    {
        var options = new DbContextOptionsBuilder<AgroSenseDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AgroSenseDbContext(options);
        _currentUserMock = new Mock<ICurrentUserProvider>();

        _useCase = new GetPropertiesFromProducerUseCase(_context, _currentUserMock.Object);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────────

    private void SetupCurrentUser(Guid userId) =>
        _currentUserMock.Setup(u => u.UserId).Returns(userId);

    private static GetFromCurrentUser BuildRequest() => new();

    private async Task<Producer> SeedProducerAsync(Guid userId)
    {
        var producer = new Producer { Id = Guid.NewGuid(), UserId = userId, Email = "prod@agro.com", CreatedAt = DateTime.UtcNow };
        _context.Producers.Add(producer);
        await _context.SaveChangesAsync();
        return producer;
    }

    private async Task<Api.Domain.Entities.Property> SeedPropertyAsync(Guid producerId, string name = "Fazenda", IEnumerable<Api.Domain.Entities.Plot>? plots = null)
    {
        var property = new Api.Domain.Entities.Property
        {
            Id = Guid.NewGuid(),
            ProducerId = producerId,
            Name = name,
            Location = "Mato Grosso",
            TotalArea = 500m,
            CreatedAt = DateTime.UtcNow,
            Plots = plots?.ToList() ?? []
        };
        _context.Properties.Add(property);
        await _context.SaveChangesAsync();
        return property;
    }

    private static Api.Domain.Entities.Plot BuildPlot(Guid propertyId, string name = "Talhão A") => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        Crop = "Soja",
        Area = 100m,
        PropertyId = propertyId,
        CreatedAt = DateTime.UtcNow
    };

    // ─── Producer not found ───────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenProducerNotFound_ReturnsProducerNotFound()
    {
        // Arrange
        SetupCurrentUser(Guid.NewGuid()); // No producer seeded

        // Act
        var result = await _useCase.Handle(BuildRequest());

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ProducerErrors.NotFound.Code, result.Error.Code);
    }

    // ─── No properties ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenProducerHasNoProperties_ReturnsSuccessWithEmptyList()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupCurrentUser(userId);
        await SeedProducerAsync(userId);

        // Act
        var result = await _useCase.Handle(BuildRequest());

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    // ─── Properties without plots ─────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenProducerHasProperties_ReturnsSuccess()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupCurrentUser(userId);
        var producer = await SeedProducerAsync(userId);
        await SeedPropertyAsync(producer.Id);

        // Act
        var result = await _useCase.Handle(BuildRequest());

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_WhenProducerHasProperties_ReturnsCorrectCount()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupCurrentUser(userId);
        var producer = await SeedProducerAsync(userId);
        await SeedPropertyAsync(producer.Id, "Fazenda A");
        await SeedPropertyAsync(producer.Id, "Fazenda B");
        await SeedPropertyAsync(producer.Id, "Fazenda C");

        // Act
        var result = await _useCase.Handle(BuildRequest());

        // Assert
        Assert.Equal(3, result.Value.Count());
    }

    [Fact]
    public async Task Handle_WhenProducerHasProperties_MapsFieldsCorrectly()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupCurrentUser(userId);
        var producer = await SeedProducerAsync(userId);
        var property = await SeedPropertyAsync(producer.Id, "Fazenda Santa Fé");

        // Act
        var result = await _useCase.Handle(BuildRequest());

        // Assert
        var response = result.Value.Single();
        Assert.Equal(property.Id, response.PropertyId);
        Assert.Equal(property.ProducerId, response.ProducerId);
        Assert.Equal(property.Name, response.Name);
        Assert.Equal(property.Location, response.Location);
        Assert.Equal(property.TotalArea, response.TotalArea);
    }

    // ─── Properties with plots ────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenPropertyHasPlots_ReturnsCorrectPlotCount()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupCurrentUser(userId);
        var producer = await SeedProducerAsync(userId);

        var propertyId = Guid.NewGuid();
        var plots = new[] { BuildPlot(propertyId, "Talhão A"), BuildPlot(propertyId, "Talhão B") };
        await SeedPropertyAsync(producer.Id, plots: plots);

        // Act
        var result = await _useCase.Handle(BuildRequest());

        // Assert
        var response = result.Value.Single();
        Assert.Equal(2, response.Plots.Count());
    }

    [Fact]
    public async Task Handle_WhenPropertyHasPlots_MapsPlotFieldsCorrectly()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupCurrentUser(userId);
        var producer = await SeedProducerAsync(userId);

        var propertyId = Guid.NewGuid();
        var plot = BuildPlot(propertyId, "Talhão Norte");
        await SeedPropertyAsync(producer.Id, plots: [plot]);

        // Act
        var result = await _useCase.Handle(BuildRequest());

        // Assert
        var plotResponse = result.Value.Single().Plots.Single();
        Assert.Equal(plot.Id, plotResponse.PlotId);
        Assert.Equal(plot.Name, plotResponse.Name);
        Assert.Equal(plot.Crop, plotResponse.Crop);
        Assert.Equal(plot.Area, plotResponse.Area);
    }

    [Fact]
    public async Task Handle_WhenPropertyHasNoPlots_ReturnsEmptyPlotList()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupCurrentUser(userId);
        var producer = await SeedProducerAsync(userId);
        await SeedPropertyAsync(producer.Id);

        // Act
        var result = await _useCase.Handle(BuildRequest());

        // Assert
        Assert.Empty(result.Value.Single().Plots);
    }

    // ─── Isolation ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ReturnsOnlyPropertiesBelongingToCurrentProducer()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupCurrentUser(userId);
        var currentProducer = await SeedProducerAsync(userId);
        await SeedPropertyAsync(currentProducer.Id, "Minha Fazenda");

        // Seed another producer with their own property
        var otherProducer = await SeedProducerAsync(Guid.NewGuid());
        await SeedPropertyAsync(otherProducer.Id, "Fazenda Alheia");

        // Act
        var result = await _useCase.Handle(BuildRequest());

        // Assert
        Assert.Single(result.Value);
        Assert.Equal("Minha Fazenda", result.Value.Single().Name);
    }
}




