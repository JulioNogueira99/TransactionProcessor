using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TransactionProcessor.Application.DTOs
{
    public sealed record CreateTransferDto(
        Guid FromAccountId,
        Guid ToAccountId,
        decimal Amount,
        string Currency,
        string ReferenceId
    );

}
