using FluentValidation;
using UniEnroll.Domain.Request;

namespace UniEnroll.Api.Validation;

public class CreateCourseValidator : AbstractValidator<CreateCourseRequest>
{
    public CreateCourseValidator()
    {
        RuleFor(x => x.Code).NotEmpty();
    }
}