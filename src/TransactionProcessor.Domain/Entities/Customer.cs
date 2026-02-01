using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TransactionProcessor.Domain.Exceptions;

namespace TransactionProcessor.Domain.Entities
{
    public class Customer
    {
        public Guid Id { get; private set; }
        public string ClientId { get; private set; }

        protected Customer() { }

        public Customer(string clientId)
        {
            if (string.IsNullOrWhiteSpace(clientId))
            {
                throw new DomainException("client_id is required");
            }

            Id = Guid.NewGuid();
            ClientId = clientId.Trim();
        }
    }
}
