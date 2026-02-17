namespace Api.Features.Plot;

public record PlotResponse(Guid PlotId, string Name, string Crop, decimal Area);
