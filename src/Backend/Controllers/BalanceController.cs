using Challenge.Api.Contracts.Responses;
using Challenge.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Challenge.Api.Controllers;

[ApiController]
[Route("api/v1/balance")]
public sealed class BalanceController : ControllerBase
{
    private readonly IAccountService _accountService;

    public BalanceController(IAccountService accountService)
    {
        _accountService = accountService;
    }

    [HttpGet]
    [ProducesResponseType<BalanceResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<BalanceResponse>> Get(CancellationToken cancellationToken)
    {
        return Ok(new BalanceResponse(await _accountService.GetBalanceAsync(cancellationToken)));
    }
}
