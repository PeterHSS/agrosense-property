using Api.Features.Plot;
using Api.Features.Property.Create;

namespace Api.Features.Property;

public static class PropertyMapper
{
    extension(CreatePropertyRequest request)
    {
        public Domain.Entities.Property ToProperty(Guid producerId)
        {
            return new Domain.Entities.Property
            {
                Id = Guid.NewGuid(),
                ProducerId = producerId,
                Name = request.Name,
                Location = request.Location,
                TotalArea = request.TotalArea,
                CreatedAt = DateTime.Now,
            };
        }
    }

    extension(Domain.Entities.Property property)
    {
        public PropertyResponse ToResponse(IEnumerable<PlotResponse> plots)
            => new(property.Id, property.ProducerId, property.Name, property.Location, property.TotalArea, plots);
    }
}
