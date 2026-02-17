using FluentValidation;

namespace Api.Features.Property.Create;

public class CreatePropertyValidator : AbstractValidator<CreatePropertyRequest>
{
    public CreatePropertyValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Property name is required.")
            .MaximumLength(500)
            .WithMessage("Property name must not exceed 500 characters.");

        RuleFor(x => x.Location)
            .NotEmpty()
            .WithMessage("Property location is required.")
            .MaximumLength(1000)
            .WithMessage("Property location must not exceed 1000 characters.");

        RuleFor(x => x.TotalArea)
            .NotEmpty()
            .WithMessage("Total area is required.")
            .GreaterThan(0)
            .WithMessage("Total area must be greater than 0.");
    }
}