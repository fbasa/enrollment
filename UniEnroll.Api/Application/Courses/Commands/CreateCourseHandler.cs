using FluentValidation;
using MediatR;
using UniEnroll.Api.DTOs;
using UniEnroll.Api.Infrastructure.Repositories;
using UniEnroll.Api.Validation;

namespace UniEnroll.Api.Application.Courses.Commands;

public record CreateCourseCommand(CreateCourseRequest Request) : IRequest<long>;

public sealed class CreateCourseCommandValidator : AbstractValidator<CreateCourseCommand>
{
    public CreateCourseCommandValidator()
    {
        RuleFor(x => x.Request).SetValidator(new CreateCourseValidator());
    }
}

public sealed class CreateCourseHandler(ICoursesRepository repo) : IRequestHandler<CreateCourseCommand, long>
{
    public async Task<long> Handle(CreateCourseCommand cmd, CancellationToken cancellationToken) =>
        await repo.CreateAsync(cmd.Request, cancellationToken);

}
