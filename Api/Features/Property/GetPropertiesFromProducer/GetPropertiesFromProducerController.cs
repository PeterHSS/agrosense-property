using Api.Common;
using Api.Domain.Abstractions.UseCases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Features.Property.GetPropertiesFromProducer;

[ApiController]
[Route("api/properties")]
public class GetPropertiesFromProducerController : ControllerBase
{
    [HttpGet("me")]
    [Authorize(Policy = Policies.UserOnly)]
    public async Task<IResult> GetPropertiesFromProducer([FromServices] IUseCase<GetFromCurrentUser, IEnumerable<PropertyResponse>> useCase)
    {
        var result = await useCase.Handle(new GetFromCurrentUser());

        if (result.IsSuccess)
            return Results.Ok(result.Value);

        return Results.NotFound(result.Error);
    }
}