using Api.Common;
using Api.Domain.Abstractions.UseCases;
using Api.Infrastructure.Persistence.Contexts;
using Api.Infrastructure.Providers;
using FluentValidation;

namespace Api.Features.Property.Create;

internal sealed class CreatePropertyUseCase(AgroSenseDbContext context, IValidator<CreatePropertyRequest> validator, ICurrentUserProvider currentUser)
    : IUseCase<CreatePropertyRequest>
{
    public async Task<Result> Handle(CreatePropertyRequest request)
    {
        var validationResult = validator.Validate(request);

        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage);

            return Result.Failure(PropertyErrors.Validation(errors));
        }

        var producer = context.Producers.FirstOrDefault(p => p.UserId == currentUser.UserId);

        if (producer is null)
        {
            producer = new Domain.Entities.Producer
            {
                Id = Guid.NewGuid(),
                UserId = currentUser.UserId,
                Email = currentUser.Email,
                CreatedAt = DateTime.UtcNow,
            };

            context.Producers.Add(producer);
        }

        var property = new Domain.Entities.Property
        {
            Id = Guid.NewGuid(),
            ProducerId = producer.Id,
            Name = request.Name,
            Location = request.Location,
            TotalArea = request.TotalArea,
            CreatedAt = DateTime.UtcNow,
        };

        context.Properties.Add(property);
        
        await context.SaveChangesAsync();

        return Result.Success();
    }
}
