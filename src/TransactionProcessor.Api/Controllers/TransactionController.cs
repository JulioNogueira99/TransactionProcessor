using Microsoft.AspNetCore.Mvc;
using TransactionProcessor.Application.DTOs;
using TransactionProcessor.Application.Interfaces;

namespace TransactionProcessor.Api.Controllers;

[ApiController]
[Route("api/transactions")]
public class TransactionsController : ControllerBase
{
    private readonly ITransactionService _service;

    public TransactionsController(ITransactionService service) => _service = service;

    [HttpPost]
    [ProducesResponseType(typeof(TransactionResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Process([FromBody] CreateTransactionDto dto, CancellationToken ct)
    {
        var result = await _service.ProcessTransactionAsync(dto, ct);
        return Ok(result);
    }
}
