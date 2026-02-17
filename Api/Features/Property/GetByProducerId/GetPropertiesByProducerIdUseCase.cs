using Api.Common;
using Api.Domain.Abstractions.UseCases;
using Api.Features.Plot;
using Api.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Api.Features.Property.GetByProducerId;

internal sealed class GetPropertiesByProducerIdUseCase(AgroSenseDbContext context) : IUseCase<GetByProducerIdRequest, IEnumerable<PropertyResponse>>
{
    public async Task<Result<IEnumerable<PropertyResponse>>> Handle(GetByProducerIdRequest request)
    {
        var properties = context.Properties.Where(p => p.ProducerId == request.ProducerId).Include(property => property.Plots);

        if (!properties.Any())
            return Result<IEnumerable<PropertyResponse>>.Success([]);

        var response = properties
            .Select(property => property.ToResponse(property.Plots.ToResponse()));

        return Result<IEnumerable<PropertyResponse>>.Success(await response.ToListAsync());
    }
}
