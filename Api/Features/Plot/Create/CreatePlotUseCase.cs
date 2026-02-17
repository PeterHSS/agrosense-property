using Api.Common;
using Api.Domain.Abstractions.UseCases;
using Api.Features.Producer;
using Api.Features.Property;
using Api.Infrastructure.Persistence.Contexts;
using Api.Infrastructure.Providers;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Api.Features.Plot.Create;

internal sealed class CreatePlotUseCase(AgroSenseDbContext context, IValidator<CreatePlotRequest> validator, ICurrentUserProvider currentUser) : IUseCase<CreatePlotRequest>
{
    public async Task<Result> Handle(CreatePlotRequest request)
    {
        var validationResult = await validator.ValidateAsync(request);

        if (!validationResult.IsValid)
            return Result.Failure(PlotErrors.Validation(validationResult.Errors.Select(e => e.ErrorMessage)));

        if (!currentUser.IsAdmin)
        {
            var producer = await context.Producers.SingleOrDefaultAsync(producer => producer.UserId == currentUser.UserId);

            if (producer is null)
                return Result.Failure(ProducerErrors.NotFound);

            var property = await context.Properties.FindAsync(request.PropertyId);

            if (property is null)
                return Result.Failure(PropertyErrors.NotFound(request.PropertyId));

            if (property.ProducerId != producer.Id)
                return Result.Failure(PropertyErrors.PropertyDoesntBelongToProducer);
        }

        var plot = new Domain.Entities.Plot
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Crop = request.Crop,
            Area = request.Area,
            PropertyId = request.PropertyId,
            CreatedAt = DateTime.UtcNow,
        };
        
        context.Plots.Add(plot);
        
        await context.SaveChangesAsync();
        
        return Result.Success();
    }
}
