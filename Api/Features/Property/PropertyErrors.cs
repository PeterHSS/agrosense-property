using Api.Common;

namespace Api.Features.Property;

public static class PropertyErrors
{
    public static Error NotFound(Guid id) => new("Property.NotFound", $"Property with id {id} was not found.");
    public static Error PropertyDoesntBelongToProducer => new("Property.PropertyDoesntBelongToProducer", "The property doesn't belong to the producer.");
    public static Error Validation(IEnumerable<string> errors) => new("Property.Validation", string.Join(';', errors));
}
