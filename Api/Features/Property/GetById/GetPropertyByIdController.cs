using Api.Common;
using Api.Domain.Abstractions.UseCases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Features.Property.GetById;

[ApiController]
[Route("api/properties")]
public class GetPropertyByIdController : ControllerBase
{
    [Authorize(Policy = Policies.UserOnly)]
    [HttpGet("{propertyId:guid}")]
    public async Task<IResult> GetPropertyById([FromRoute] Guid propertyId, [FromServices] IUseCase<GetPropertyByIdRequest, PropertyResponse> useCase)
    {
        var result = await useCase.Handle(new GetPropertyByIdRequest(propertyId));

        if (result.IsSuccess)
            return Results.Ok(result.Value);

        return Results.NotFound(result.Error);
    }
}
