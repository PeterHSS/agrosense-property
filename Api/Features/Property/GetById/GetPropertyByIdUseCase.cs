using Api.Common;
using Api.Domain.Abstractions.UseCases;
using Api.Features.Plot;
using Api.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Api.Features.Property.GetById;

internal sealed class GetPropertyByIdUseCase(AgroSenseDbContext context) : IUseCase<GetPropertyByIdRequest, PropertyResponse>
{
    public async Task<Result<PropertyResponse>> Handle(GetPropertyByIdRequest request)
    {
        var property = context.Properties.Find(request.PropertyId);

        if (property is null)
            return Result<PropertyResponse>.Failure(PropertyErrors.NotFound(request.PropertyId));

        var plots = await context.Plots.AsNoTracking().Where(p => p.PropertyId == property.Id).ToListAsync();

        var response = property.ToResponse(plots.ToResponse());

        return Result<PropertyResponse>.Success(response);
    }
}
