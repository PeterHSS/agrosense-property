using Api.Features.Plot;

namespace Api.Features.Property;

public record PropertyResponse(Guid PropertyId, Guid ProducerId, string Name, string Location, decimal TotalArea, IEnumerable<PlotResponse> Plots);
