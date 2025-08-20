using FluentValidation;
using UniEnroll.Domain.Request;

namespace UniEnroll.Application.Validators;

public sealed class OfferingUpsertValidator : AbstractValidator<OfferingUpsertRequest>
{
    public OfferingUpsertValidator()
    {
        RuleFor(x => x.TermId).GreaterThan(0);
        RuleFor(x => x.CourseId).GreaterThan(0);
        RuleFor(x => x.Section).NotEmpty().MaximumLength(8);
        RuleFor(x => x.RoomId).GreaterThan(0);
        RuleFor(x => x.Capacity).InclusiveBetween(1, 500);
        RuleFor(x => x.WaitlistCapacity).GreaterThanOrEqualTo(0);
        RuleFor(x => x.TimeSlotIds).NotEmpty().Must(ts => ts.Distinct().Count() == ts.Count)
            .WithMessage("Duplicate TimeSlotIds not allowed.");
    }
}
