using FluentValidation;
using MediatR;
using UniEnroll.Api.Common;
using UniEnroll.Api.DTOs;
using UniEnroll.Api.Infrastructure.Repositories;
using UniEnroll.Api.Validation;

namespace UniEnroll.Api.Application.Enrollments.Commands;

public record EnrollCommand(EnrollRequest Request, long ActorUserId) : IRequest<EnrollResponse>, ITransactionalRequest;

public sealed class EnrollCommandValidator : AbstractValidator<EnrollCommand>
{
    public EnrollCommandValidator() => RuleFor(x => x.Request).SetValidator(new EnrollRequestValidator());
}

public sealed class EnrollHandler(IEnrollmentsRepository repo) : IRequestHandler<EnrollCommand, EnrollResponse>
{
    public async Task<EnrollResponse> Handle(EnrollCommand cmd, CancellationToken ct)
    {
        await repo.SetActorAsync(cmd.ActorUserId, ct);

        var r = cmd.Request;
        var errors = await repo.ValidateAsync(r.StudentId, r.OfferingId, ct);
        if (errors.Any()) throw new ValidationException(string.Join(" ", errors));

        var seat = await repo.SeatSnapshotForUpdateAsync(r.OfferingId, ct);
        if (seat is null) throw new ValidationException("Offering not found.");
        var (capacity, waitCap, enrolled, waitlisted) = seat.Value;

        var status = enrolled < capacity ? "Enrolled" : waitlisted < waitCap ? "Waitlisted" : null;
        if (status is null) throw new ValidationException("Class full.");

        var id = await repo.CreateAsync(r.StudentId, r.OfferingId, status, ct);
        return new EnrollResponse(id, status);
    }
}