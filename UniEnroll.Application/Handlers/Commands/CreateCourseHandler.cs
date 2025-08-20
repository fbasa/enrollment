using FluentValidation;
using MediatR;
using UniEnroll.Infrastructure.Repositories;
using UniEnroll.Domain.Request;
using UniEnroll.Application.Validators;

namespace UniEnroll.Application.Handlers.Commands;

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
