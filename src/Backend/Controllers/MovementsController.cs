using Challenge.Api.Contracts.Requests;
using Challenge.Api.Contracts.Responses;
using Challenge.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Challenge.Api.Controllers;

[ApiController]
[Route("api/v1/movements")]
public sealed class MovementsController : ControllerBase
{
    private readonly IAccountService _accountService;

    public MovementsController(IAccountService accountService)
    {
        _accountService = accountService;
    }

    [HttpPost]
    [ProducesResponseType<MovementResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MovementResponse>> Create(
        CreateMovementRequest request,
        CancellationToken cancellationToken)
    {
        var movement = await _accountService.CreateMovementAsync(
            request.Type!.Value,
            request.Amount!.Value,
            cancellationToken);

        return StatusCode(StatusCodes.Status201Created, MovementResponse.FromDomain(movement));
    }

    [HttpGet]
    [ProducesResponseType<IReadOnlyCollection<MovementResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<MovementResponse>>> GetAll(
        CancellationToken cancellationToken)
    {
        var movements = await _accountService.GetHistoryAsync(cancellationToken);
        return Ok(movements.Select(MovementResponse.FromDomain).ToArray());
    }
}
