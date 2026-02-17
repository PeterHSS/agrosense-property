using Api.Common;

namespace Api.Features.Producer;

public static class ProducerErrors
{
    public static Error NotFound => new("Producer.NotFound", "Producer not found.");
}
