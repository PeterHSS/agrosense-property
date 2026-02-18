using FluentValidation;

namespace Api.Features.Plot.Create;

internal sealed class CreatePlotValidator : AbstractValidator<CreatePlotRequest>
{
    public CreatePlotValidator()
    {
        RuleFor(request => request.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(100).WithMessage("Name must not exceed 100 characters.");

        RuleFor(request => request.Crop)
            .NotEmpty().WithMessage("Crop is required.")
            .MaximumLength(100).WithMessage("Crop must not exceed 100 characters.");

        RuleFor(request => request.Area)
            .GreaterThan(0).WithMessage("Area must be greater than zero.");
    }
}
