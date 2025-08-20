using FluentValidation;
using MediatR;
using UniEnroll.Application.Validators;
using UniEnroll.Domain.Request;
using UniEnroll.Domain.Response;
using UniEnroll.Infrastructure.Repositories;

namespace UniEnroll.Application.Handlers.Commands;

public record EnrollCommand(EnrollRequest Request, long ActorUserId) : IRequest<EnrollResponse>, ITransactionalRequest, IIdempotentRequest;

public sealed class EnrollCommandValidator : AbstractValidator<EnrollCommand>
{
    public EnrollCommandValidator() => RuleFor(x => x.Request).SetValidator(new EnrollRequestValidator());
}

public sealed class EnrollHandler(IEnrollmentsRepository repo, IMediator mediator) : IRequestHandler<EnrollCommand, EnrollResponse>
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

        //SignalR
        //var evt = new EnrollmentEventDto(id, offeringId, termId, studentId, finalStatus, DateTime.UtcNow);
        //var counts = await repo2(offeringId, ct); // returns OfferingSeatCountsDto
        //await mediator.Publish(new EnrollmentCommitted(evt, counts), ct);

        return new EnrollResponse(id, status);
    }
}