using Api.Common;
using Api.Domain.Abstractions.UseCases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Features.Plot.Create;

[ApiController]
[Route("api/plot")]
public class CreatePlotController : ControllerBase
{
    [HttpPost]
    [Authorize(Policy = Policies.UserOnly)]
    public async Task<IResult> Create([FromBody] CreatePlotRequest request, [FromServices] IUseCase<CreatePlotRequest> useCase)
    {
        var result = await useCase.Handle(request);

        if (result.IsSuccess)
            return Results.Created();

        return Results.BadRequest(result.Error);
    }
}
