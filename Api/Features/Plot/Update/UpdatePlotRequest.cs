namespace Api.Features.Plot.Update;

public record UpdatePlotRequest(Guid PlotId, string Name, string Crop, decimal Area);