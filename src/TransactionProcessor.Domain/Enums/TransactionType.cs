using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TransactionProcessor.Domain.Enums
{
    public enum TransactionType
    {
        Credit = 1,
        Debit = 2,
        Reserve = 3,
        Capture = 4,
        Reversal = 5,
        Transfer = 6
    }
}
