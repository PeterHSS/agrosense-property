using Api.Domain.Entities;
using Api.Features.Plot;
using Api.Features.Plot.Create;
using Api.Features.Producer;
using Api.Features.Property;
using Api.Infrastructure.Persistence.Contexts;
using Api.Infrastructure.Providers;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Test.Features.Plot;

public class CreatePlotUseCaseTests
{
    private readonly AgroSenseDbContext _context;
    private readonly Mock<IValidator<CreatePlotRequest>> _validatorMock;
    private readonly Mock<ICurrentUserProvider> _currentUserMock;
    private readonly CreatePlotUseCase _useCase;

    public CreatePlotUseCaseTests()
    {
        var options = new DbContextOptionsBuilder<AgroSenseDbContext>().UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()).Options;
        _context = new AgroSenseDbContext(options);
        _validatorMock = new Mock<IValidator<CreatePlotRequest>>();
        _currentUserMock = new Mock<ICurrentUserProvider>();
        _useCase = new CreatePlotUseCase(_context, _validatorMock.Object, _currentUserMock.Object);
    }

    private void SetupValidRequest() =>
        _validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<CreatePlotRequest>(), default))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

    private void SetupInvalidRequest(params string[] errors) =>
        _validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<CreatePlotRequest>(), default))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult(errors.Select(e => new FluentValidation.Results.ValidationFailure("Field", e))));

    private static CreatePlotRequest BuildRequest(Guid? propertyId = null) => new("Talhão Norte", "Soja", 100.5m, propertyId ?? Guid.NewGuid());

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

    [Fact]
    public async Task Handle_WhenValidationFails_ReturnsFailureWithErrors()
    {
        // Arrange
        SetupInvalidRequest("Name is required", "Area must be positive");
        var request = BuildRequest();

        // Act
        var result = await _useCase.Handle(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(PlotErrors.Validation(["Name is required", "Area must be positive"]).Code, result.Error.Code);
    }

    [Fact]
    public async Task Handle_WhenValidationFails_DoesNotPersistAnything()
    {
        // Arrange
        SetupInvalidRequest("Name is required");
        var request = BuildRequest();

        // Act
        await _useCase.Handle(request);

        // Assert
        Assert.Empty(_context.Plots);
    }


    [Fact]
    public async Task Handle_WhenAdmin_SkipsProducerAndPropertyChecks_AndSucceeds()
    {
        // Arrange
        SetupValidRequest();
        _currentUserMock.Setup(u => u.IsAdmin).Returns(true);
        var request = BuildRequest();

        // Act
        var result = await _useCase.Handle(request);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_WhenAdmin_PersistsPlotWithCorrectFields()
    {
        // Arrange
        SetupValidRequest();
        _currentUserMock.Setup(u => u.IsAdmin).Returns(true);
        var propertyId = Guid.NewGuid();
        var request = BuildRequest(propertyId);

        // Act
        await _useCase.Handle(request);

        // Assert
        var plot = _context.Plots.Single();
        Assert.Equal(request.Name, plot.Name);
        Assert.Equal(request.Crop, plot.Crop);
        Assert.Equal(request.Area, plot.Area);
        Assert.Equal(propertyId, plot.PropertyId);
        Assert.NotEqual(Guid.Empty, plot.Id);
    }

    [Fact]
    public async Task Handle_WhenAdmin_SetsCreatedAtToUtcNow()
    {
        // Arrange
        SetupValidRequest();
        _currentUserMock.Setup(u => u.IsAdmin).Returns(true);
        var before = DateTime.UtcNow;

        // Act
        await _useCase.Handle(BuildRequest());

        // Assert
        var after = DateTime.UtcNow;
        var plot = _context.Plots.Single();
        Assert.InRange(plot.CreatedAt, before, after);
    }


    [Fact]
    public async Task Handle_WhenNotAdmin_AndProducerNotFound_ReturnsProducerNotFound()
    {
        // Arrange
        SetupValidRequest();
        var userId = Guid.NewGuid();
        _currentUserMock.Setup(u => u.IsAdmin).Returns(false);
        _currentUserMock.Setup(u => u.UserId).Returns(userId);
        // No producer seeded

        // Act
        var result = await _useCase.Handle(BuildRequest());

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ProducerErrors.NotFound.Code, result.Error.Code);
    }


    [Fact]
    public async Task Handle_WhenNotAdmin_AndPropertyNotFound_ReturnsPropertyNotFound()
    {
        // Arrange
        SetupValidRequest();
        var userId = Guid.NewGuid();
        _currentUserMock.Setup(u => u.IsAdmin).Returns(false);
        _currentUserMock.Setup(u => u.UserId).Returns(userId);

        await SeedProducerAsync(userId);

        var nonExistentPropertyId = Guid.NewGuid();
        var request = BuildRequest(nonExistentPropertyId);

        // Act
        var result = await _useCase.Handle(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(PropertyErrors.NotFound(nonExistentPropertyId).Code, result.Error.Code);
    }

    [Fact]
    public async Task Handle_WhenNotAdmin_AndPropertyBelongsToOtherProducer_ReturnsPropertyDoesntBelongToProducer()
    {
        // Arrange
        SetupValidRequest();
        var userId = Guid.NewGuid();
        _currentUserMock.Setup(u => u.IsAdmin).Returns(false);
        _currentUserMock.Setup(u => u.UserId).Returns(userId);

        await SeedProducerAsync(userId); // current producer

        var anotherProducerId = Guid.NewGuid();
        var property = await SeedPropertyAsync(anotherProducerId); // belongs to someone else

        var request = BuildRequest(property.Id);

        // Act
        var result = await _useCase.Handle(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(PropertyErrors.PropertyDoesntBelongToProducer.Code, result.Error.Code);
    }

    [Fact]
    public async Task Handle_WhenNotAdmin_AndEverythingValid_ReturnsSuccess()
    {
        // Arrange
        SetupValidRequest();
        var userId = Guid.NewGuid();
        _currentUserMock.Setup(u => u.IsAdmin).Returns(false);
        _currentUserMock.Setup(u => u.UserId).Returns(userId);

        var producer = await SeedProducerAsync(userId);
        var property = await SeedPropertyAsync(producer.Id);

        var request = BuildRequest(property.Id);

        // Act
        var result = await _useCase.Handle(request);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_WhenNotAdmin_AndEverythingValid_PersistsPlotCorrectly()
    {
        // Arrange
        SetupValidRequest();
        var userId = Guid.NewGuid();
        _currentUserMock.Setup(u => u.IsAdmin).Returns(false);
        _currentUserMock.Setup(u => u.UserId).Returns(userId);

        var producer = await SeedProducerAsync(userId);
        var property = await SeedPropertyAsync(producer.Id);

        var request = BuildRequest(property.Id);

        // Act
        await _useCase.Handle(request);

        // Assert
        var plot = _context.Plots.Single();
        Assert.Equal(request.Name, plot.Name);
        Assert.Equal(request.Crop, plot.Crop);
        Assert.Equal(request.Area, plot.Area);
        Assert.Equal(property.Id, plot.PropertyId);
        Assert.NotEqual(Guid.Empty, plot.Id);
    }

    [Fact]
    public async Task Handle_WhenNotAdmin_AndEverythingValid_DoesNotPersistMoreThanOnePlot()
    {
        // Arrange
        SetupValidRequest();
        var userId = Guid.NewGuid();
        _currentUserMock.Setup(u => u.IsAdmin).Returns(false);
        _currentUserMock.Setup(u => u.UserId).Returns(userId);

        var producer = await SeedProducerAsync(userId);
        var property = await SeedPropertyAsync(producer.Id);

        // Act
        await _useCase.Handle(BuildRequest(property.Id));

        // Assert
        Assert.Single(_context.Plots);
    }
}