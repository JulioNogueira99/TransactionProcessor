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
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateAccountDto dto, CancellationToken ct)
    {
        var created = await _service.CreateAccountAsync(dto, ct);

        return CreatedAtAction(nameof(GetById), new { id = created.AccountId }, created);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AccountResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var account = await _service.GetAccountAsync(id, ct);

        if (account is null)
            return NotFound(Problem(title: "Account not found", statusCode: StatusCodes.Status404NotFound));

        return Ok(account);
    }
}
