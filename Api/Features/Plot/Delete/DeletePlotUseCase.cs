using Api.Common;
using Api.Domain.Abstractions.UseCases;
using Api.Features.Producer;
using Api.Infrastructure.Persistence.Contexts;
using Api.Infrastructure.Providers;
using Microsoft.EntityFrameworkCore;

namespace Api.Features.Plot.Delete;

internal sealed class DeletePlotUseCase(AgroSenseDbContext context, ICurrentUserProvider currentUser) : IUseCase<DeletePlotRequest>
{
    public async Task<Result> Handle(DeletePlotRequest request)
    {
        var producer = await context.Producers.SingleOrDefaultAsync(producer => producer.UserId == currentUser.UserId);

        if (producer is null)
            return Result.Failure(ProducerErrors.NotFound);

        var plot = await context.Plots.FindAsync(request.PlotId);

        if (plot is null)
            return Result.Failure(PlotErrors.NotFound(request.PlotId));

        if (plot.Property.ProducerId != producer.Id)
            return Result.Failure(PlotErrors.PlotDoesntBelongToProducer);

        context.Plots.Remove(plot);
        
        await context.SaveChangesAsync();
        
        return Result.Success();
    }
}
