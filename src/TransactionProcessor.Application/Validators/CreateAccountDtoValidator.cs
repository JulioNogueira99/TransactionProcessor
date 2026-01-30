using FluentValidation;
using TransactionProcessor.Application.DTOs;

namespace TransactionProcessor.Application.Validators;

public sealed class CreateAccountDtoValidator : AbstractValidator<CreateAccountDto>
{
    public CreateAccountDtoValidator()
    {
        RuleFor(x => x.CreditLimit)
            .GreaterThanOrEqualTo(0).WithMessage("credit_limit must be >= 0.");
    }
}