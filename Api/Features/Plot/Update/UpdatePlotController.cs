using Api.Common;
using Api.Domain.Abstractions.UseCases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Features.Plot.Update;

[ApiController]
[Route("api/plots")]
public class UpdatePlotController : ControllerBase
{
    [HttpPatch]
    [Authorize(Policy = Policies.UserOnly)]
    public async Task<IResult> UpdatePlot([FromBody] UpdatePlotRequest request, [FromServices] IUseCase<UpdatePlotRequest> useCase)
    {
        var result = await useCase.Handle(request);

        if (result.IsSuccess)
            return Results.NoContent();
        
        return Results.BadRequest(result.Error);
    }
}
