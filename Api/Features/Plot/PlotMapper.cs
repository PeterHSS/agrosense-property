namespace Api.Features.Plot;

public static class PlotMapper
{
    extension(Domain.Entities.Plot plot)
    {
        public PlotResponse ToResponse()
            => new(plot.Id, plot.Name, plot.Crop, plot.Area);
    }

    extension(IEnumerable<Domain.Entities.Plot> plots)
    {
        public IEnumerable<PlotResponse> ToResponse()
            => plots.Select(ToResponse);
    }
}