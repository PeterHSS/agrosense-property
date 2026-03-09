using Api.Domain.Entities;
using Api.Features.Property;
using Api.Features.Property.Create;
using Api.Infrastructure.Persistence.Contexts;
using Api.Infrastructure.Providers;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Test.Features.Property;

public class CreatePropertyUseCaseTests
{
    private readonly AgroSenseDbContext _context;
    private readonly Mock<IValidator<CreatePropertyRequest>> _validatorMock;
    private readonly Mock<ICurrentUserProvider> _currentUserMock;
    private readonly CreatePropertyUseCase _useCase;

    public CreatePropertyUseCaseTests()
    {
        var options = new DbContextOptionsBuilder<AgroSenseDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AgroSenseDbContext(options);
        _validatorMock = new Mock<IValidator<CreatePropertyRequest>>();
        _currentUserMock = new Mock<ICurrentUserProvider>();

        _useCase = new CreatePropertyUseCase(_context, _validatorMock.Object, _currentUserMock.Object);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────────

    private void SetupValidRequest() =>
        _validatorMock
            .Setup(v => v.Validate(It.IsAny<CreatePropertyRequest>()))
            .Returns(new ValidationResult());

    private void SetupInvalidRequest(params string[] errors) =>
        _validatorMock
            .Setup(v => v.Validate(It.IsAny<CreatePropertyRequest>()))
            .Returns(new ValidationResult(errors.Select(e => new ValidationFailure("Field", e))));

    private void SetupCurrentUser(Guid? userId = null, string email = "produtor@agro.com")
    {
        _currentUserMock.Setup(u => u.UserId).Returns(userId ?? Guid.NewGuid());
        _currentUserMock.Setup(u => u.Email).Returns(email);
    }

    private static CreatePropertyRequest BuildRequest() =>
        new("Fazenda Santa Fé", "Mato Grosso", 500m);

    private async Task<Producer> SeedProducerAsync(Guid userId, string email = "existente@agro.com")
    {
        var producer = new Producer
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Email = email,
            CreatedAt = DateTime.UtcNow
        };
        _context.Producers.Add(producer);
        await _context.SaveChangesAsync();
        return producer;
    }

    // ─── Validation failures ──────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenValidationFails_ReturnsFailureWithErrors()
    {
        // Arrange
        SetupInvalidRequest("Name is required", "TotalArea must be positive");
        SetupCurrentUser();

        // Act
        var result = await _useCase.Handle(BuildRequest());

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(PropertyErrors.Validation(["Name is required", "TotalArea must be positive"]).Code, result.Error.Code);
    }

    [Fact]
    public async Task Handle_WhenValidationFails_DoesNotPersistAnything()
    {
        // Arrange
        SetupInvalidRequest("Name is required");
        SetupCurrentUser();

        // Act
        await _useCase.Handle(BuildRequest());

        // Assert
        Assert.Empty(_context.Properties);
        Assert.Empty(_context.Producers);
    }

    // ─── Producer auto-creation ───────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenProducerDoesNotExist_CreatesProducerAutomatically()
    {
        // Arrange
        SetupValidRequest();
        var userId = Guid.NewGuid();
        SetupCurrentUser(userId, "novo@agro.com");

        // Act
        await _useCase.Handle(BuildRequest());

        // Assert
        var producer = _context.Producers.SingleOrDefault(p => p.UserId == userId);
        Assert.NotNull(producer);
    }

    [Fact]
    public async Task Handle_WhenProducerDoesNotExist_CreatesProducerWithCorrectFields()
    {
        // Arrange
        SetupValidRequest();
        var userId = Guid.NewGuid();
        SetupCurrentUser(userId, "novo@agro.com");
        var before = DateTime.UtcNow;

        // Act
        await _useCase.Handle(BuildRequest());

        // Assert
        var after = DateTime.UtcNow;
        var producer = _context.Producers.Single(p => p.UserId == userId);
        Assert.Equal(userId, producer.UserId);
        Assert.Equal("novo@agro.com", producer.Email);
        Assert.NotEqual(Guid.Empty, producer.Id);
        Assert.InRange(producer.CreatedAt, before, after);
    }

    [Fact]
    public async Task Handle_WhenProducerAlreadyExists_DoesNotCreateDuplicateProducer()
    {
        // Arrange
        SetupValidRequest();
        var userId = Guid.NewGuid();
        SetupCurrentUser(userId);
        await SeedProducerAsync(userId);

        // Act
        await _useCase.Handle(BuildRequest());

        // Assert
        Assert.Single(_context.Producers);
    }

    [Fact]
    public async Task Handle_WhenProducerAlreadyExists_ReusesExistingProducer()
    {
        // Arrange
        SetupValidRequest();
        var userId = Guid.NewGuid();
        SetupCurrentUser(userId);
        var existing = await SeedProducerAsync(userId);

        // Act
        await _useCase.Handle(BuildRequest());

        // Assert
        var property = _context.Properties.Single();
        Assert.Equal(existing.Id, property.ProducerId);
    }

    // ─── Property creation ────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenEverythingValid_PersistsPropertyWithCorrectFields()
    {
        // Arrange
        SetupValidRequest();
        var userId = Guid.NewGuid();
        SetupCurrentUser(userId);
        var request = BuildRequest();

        // Act
        await _useCase.Handle(request);

        // Assert
        var property = _context.Properties.Single();
        Assert.Equal(request.Name, property.Name);
        Assert.Equal(request.Location, property.Location);
        Assert.Equal(request.TotalArea, property.TotalArea);
        Assert.NotEqual(Guid.Empty, property.Id);
    }

    [Fact]
    public async Task Handle_WhenEverythingValid_SetsPropertyCreatedAtToUtcNow()
    {
        // Arrange
        SetupValidRequest();
        SetupCurrentUser();
        var before = DateTime.UtcNow;

        // Act
        await _useCase.Handle(BuildRequest());

        // Assert
        var after = DateTime.UtcNow;
        var property = _context.Properties.Single();
        Assert.InRange(property.CreatedAt, before, after);
    }

    [Fact]
    public async Task Handle_WhenEverythingValid_LinksPropertyToCorrectProducer()
    {
        // Arrange
        SetupValidRequest();
        var userId = Guid.NewGuid();
        SetupCurrentUser(userId);
        var producer = await SeedProducerAsync(userId);

        // Act
        await _useCase.Handle(BuildRequest());

        // Assert
        var property = _context.Properties.Single();
        Assert.Equal(producer.Id, property.ProducerId);
    }

    // ─── Happy path ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenEverythingValid_ReturnsSuccess()
    {
        // Arrange
        SetupValidRequest();
        SetupCurrentUser();

        // Act
        var result = await _useCase.Handle(BuildRequest());

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_WhenCalledTwice_CreatesTwoProperties()
    {
        // Arrange
        SetupValidRequest();
        var userId = Guid.NewGuid();
        SetupCurrentUser(userId);

        // Act
        await _useCase.Handle(BuildRequest());
        await _useCase.Handle(BuildRequest());

        // Assert
        Assert.Equal(2, _context.Properties.Count());
        Assert.Single(_context.Producers); // producer reused
    }
}