using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TransactionProcessor.Application.DTOs;
using TransactionProcessor.Application.Helpers;
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
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Process([FromBody] CreateTransactionDto dto, CancellationToken ct)
    {
        try
        {
            var result = await _service.ProcessTransactionAsync(dto, ct);

            if (string.Equals(result.Status, "failed", StringComparison.OrdinalIgnoreCase))
            {
                if (string.Equals(result.ErrorMessage, "Account not found", StringComparison.OrdinalIgnoreCase))
                {
                    return NotFound(Problem(
                        title: "Account not found",
                        detail: result.ErrorMessage,
                        statusCode: StatusCodes.Status404NotFound));
                }

                if (result.ErrorMessage?.StartsWith("Unknown operation:", StringComparison.OrdinalIgnoreCase) == true)
                {
                    return BadRequest(Problem(
                        title: "Invalid operation",
                        detail: result.ErrorMessage,
                        statusCode: StatusCodes.Status400BadRequest));
                }

                return UnprocessableEntity(Problem(
                    title: "Business rule violation",
                    detail: result.ErrorMessage,
                    statusCode: StatusCodes.Status422UnprocessableEntity));
            }

            return Ok(result);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(Problem(
                title: "Concurrency conflict",
                detail: "The account was updated by another operation. Please retry.",
                statusCode: StatusCodes.Status409Conflict));
        }
        catch (Exception ex) when (SqlServerErrors.IsDeadlockOrTimeout(ex))
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, Problem(
                title: "Temporary failure",
                detail: "A transient database error occurred. Please retry.",
                statusCode: StatusCodes.Status503ServiceUnavailable));
        }
    }
}
