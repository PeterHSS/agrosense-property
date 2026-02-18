
using Api.Common;
using Api.Domain.Abstractions.UseCases;
using Api.Features.Plot;
using Api.Features.Plot.Update;
using Api.Infrastructure.Persistence.Contexts;
using Api.Infrastructure.Providers;
using Microsoft.EntityFrameworkCore;

public class UpdatePlotUseCase(AgroSenseDbContext context, ICurrentUserProvider currentUser) : IUseCase<UpdatePlotRequest>
{
    public async Task<Result> Handle(UpdatePlotRequest request)
    {
        var plot = await context.Plots.FindAsync(request.PlotId);

        if (plot is null)
            return Result.Failure(PlotErrors.NotFound(request.PlotId));

        var isOwner = await context.Plots.AnyAsync(p => p.Id == request.PlotId && p.Property.Producer.UserId == currentUser.UserId);

        if (!isOwner)
            return Result.Failure(PlotErrors.ForbbidenOperation);

        plot.Name = request.Name ?? plot.Name;
        plot.Crop = request.Crop ?? plot.Crop;
        plot.Area = request.Area != default ? request.Area : plot.Area;

        context.Plots.Update(plot);

        await context.SaveChangesAsync();

        return Result.Success();
    }
}
