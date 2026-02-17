namespace Api.Features.Plot.Create;

public record CreatePlotRequest(string Name, string Crop, decimal Area, Guid PropertyId);