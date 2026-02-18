using Api.Common;
using Api.Domain.Abstractions.UseCases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Features.Plot.Delete;

[ApiController]
[Route("api/plots")]
[Authorize(Policy = Policies.UserOnly)]
public class DeletePlotController : ControllerBase 
{
    [HttpDelete("plotId")]
    public async Task<IResult> Delete([FromRoute] Guid plotId, [FromServices] IUseCase<DeletePlotRequest> useCase)
    {
        var result = await useCase.Handle(new DeletePlotRequest(plotId));

        if (result.IsSuccess)
            return Results.NoContent();

        return Results.BadRequest(result.Error);
    }
}
