using FluentValidation;
using UniEnroll.Api.DTOs;

namespace UniEnroll.Api.Validation;

public sealed class EnrollRequestValidator : AbstractValidator<EnrollRequest>
{
    public EnrollRequestValidator()
    {
        RuleFor(x => x.StudentId).GreaterThan(0);
        RuleFor(x => x.OfferingId).GreaterThan(0);
    }
}
