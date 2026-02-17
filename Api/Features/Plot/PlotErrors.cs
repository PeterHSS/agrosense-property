using Api.Common;

namespace Api.Features.Plot;

public static class PlotErrors
{
    public static Error Validation(IEnumerable<string> errors) => new("Plot.Validation", string.Join(';', errors));
    public static Error NotFound(Guid id) => new("Plot.NotFound", $"Plot with id {id} was not found.");
    public static Error PlotDoesntBelongToCurrentUser => new("Plot.PlotDoesntBelongToCurrentUser", "The plot doesn't belong to the current user.");
}
