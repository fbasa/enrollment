using FluentValidation;
using UniEnroll.Api.DTOs;

namespace UniEnroll.Api.Validation;

public class CreateCourseValidator : AbstractValidator<CreateCourseRequest>
{
    public CreateCourseValidator()
    {
        RuleFor(x => x.Code).NotEmpty();
    }
}