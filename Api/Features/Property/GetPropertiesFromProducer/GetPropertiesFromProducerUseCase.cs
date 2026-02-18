using Api.Common;
using Api.Domain.Abstractions.UseCases;
using Api.Features.Plot;
using Api.Features.Producer;
using Api.Infrastructure.Persistence.Contexts;
using Api.Infrastructure.Providers;
using Microsoft.EntityFrameworkCore;

namespace Api.Features.Property.GetPropertiesFromProducer;

internal sealed class GetPropertiesFromProducerUseCase(AgroSenseDbContext context, ICurrentUserProvider currentUser) : IUseCase<GetFromCurrentUser, IEnumerable<PropertyResponse>>
{
    public async Task<Result<IEnumerable<PropertyResponse>>> Handle(GetFromCurrentUser request)
    {
        var producer = await context.Producers.SingleOrDefaultAsync(pr => pr.UserId == currentUser.UserId);

        if (producer is null)
            return Result<IEnumerable<PropertyResponse>>.Failure(ProducerErrors.NotFound);

        var properties = await context.Properties.AsNoTracking().Where(p => p.ProducerId == producer.Id).Include(pr => pr.Plots).ToListAsync();

        if (properties.Count == 0)
            return Result < IEnumerable<PropertyResponse>>.Success([]);

        var response = properties
            .Select(p => new PropertyResponse(p.Id, p.ProducerId, p.Name, p.Location, p.TotalArea, p.Plots.Select(pl => new PlotResponse(pl.Id, pl.Name, pl.Crop, pl.Area)))).ToList();

        return Result<IEnumerable<PropertyResponse>>.Success(response);
    }
}
