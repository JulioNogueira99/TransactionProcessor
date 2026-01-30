using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TransactionProcessor.Application.Interfaces
{
    public interface IAccountLock
    {
        Task AcquireAsync(Guid accountId, CancellationToken ct);
    }
}
