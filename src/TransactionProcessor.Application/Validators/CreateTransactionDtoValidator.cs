using FluentValidation;
using TransactionProcessor.Application.DTOs;

namespace TransactionProcessor.Application.Validators
{
    public sealed class CreateTransactionDtoValidator : AbstractValidator<CreateTransactionDto>
    {
        private static readonly string[] AllowedOperations = ["credit", "debit", "reserve", "capture", "transfer"];

        public CreateTransactionDtoValidator()
        {
            RuleFor(x => x.AccountId)
                .NotEmpty().WithMessage("account_id is required.");

            RuleFor(x => x.Amount)
                .GreaterThan(0).WithMessage("amount must be greater than 0.");

            RuleFor(x => x.Currency)
                .NotEmpty().WithMessage("currency is required.")
                .Length(3).WithMessage("currency must be exactly 3 characters.")
                .Matches("^[A-Za-z]{3}$").WithMessage("currency must contain only letters.")
                .Must(c => c.ToUpperInvariant() == c).WithMessage("currency must be uppercase (e.g. BRL, USD).");

            RuleFor(x => x.ReferenceId)
                .NotEmpty().WithMessage("reference_id is required.")
                .MinimumLength(3).WithMessage("reference_id must have at least 3 characters.")
                .MaximumLength(64).WithMessage("reference_id must have at most 64 characters.")
                .Matches("^[A-Za-z0-9\\-_.]+$").WithMessage("reference_id contains invalid characters.");

            RuleFor(x => x.Operation)
                .NotEmpty().WithMessage("operation is required.")
                .Must(op => AllowedOperations.Contains(op.Trim().ToLowerInvariant()))
                .WithMessage($"operation must be one of: {string.Join(", ", AllowedOperations)}.");

            RuleFor(x => x.DestinationAccountId)
                .NotNull()
                .When(x => x.Operation.Trim().Equals("transfer", StringComparison.OrdinalIgnoreCase))
                .WithMessage("destination_account_id is required for transfer.");
        }
    }
}
