using Microsoft.AspNetCore.Mvc;
using TransactionProcessor.Application.DTOs;
using TransactionProcessor.Application.Interfaces;

namespace TransactionProcessor.Api.Controllers;

[ApiController]
[Route("api/accounts")]
public class AccountsController : ControllerBase
{
    private readonly IAccountService _service;

    public AccountsController(IAccountService service) => _service = service;

    [HttpPost]
    [ProducesResponseType(typeof(AccountResultDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateAccountDto dto, CancellationToken ct)
    {
        var result = await _service.CreateAccountAsync(dto, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.AccountId }, result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AccountResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _service.GetAccountAsync(id, ct);
        return result is null ? NotFound() : Ok(result);
    }
}
