using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TransactionProcessor.Application.Interfaces
{
    public interface IEventPublisher
    {
        Task PublishAsync(string type, string payload, CancellationToken ct);
    }
}
