using Api.Common;
using Api.Domain.Abstractions.UseCases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Features.Property.Create;

[ApiController]
[Route("api/properties")]
public class CreatePropertyController : ControllerBase
{
    [HttpPost]
    [Authorize(Policy = Policies.UserOnly)]
    public async Task<IResult> CreateProperty([FromBody] CreatePropertyRequest request, [FromServices] IUseCase<CreatePropertyRequest> useCase)
    {
        var result = await useCase.Handle(request);
     
        if (result.IsSuccess)
            return Results.Created();
        
        return Results.BadRequest(result.Error);
    }
}
