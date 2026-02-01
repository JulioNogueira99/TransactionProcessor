using FluentValidation;
using TransactionProcessor.Application.DTOs;

namespace TransactionProcessor.Application.Validators;

public sealed class CreateAccountDtoValidator : AbstractValidator<CreateAccountDto>
{
    public CreateAccountDtoValidator()
    {
        RuleFor(x => x.ClientId)
            .NotEmpty().WithMessage("client_id is required.")
            .MinimumLength(3).WithMessage("client_id must have at least 3 characters.")
            .MaximumLength(32).WithMessage("client_id must have at most 32 characters.")
            .Matches("^[A-Za-z0-9\\-_.]+$").WithMessage("client_id contains invalid characters.");

        RuleFor(x => x.CreditLimit)
            .GreaterThanOrEqualTo(0).WithMessage("credit_limit must be >= 0.");
    }
}