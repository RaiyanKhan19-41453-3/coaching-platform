using CoachingPlatform.Application.Common;
using CoachingPlatform.Domain.Common;
using CoachingPlatform.Domain.Scheduling;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CoachingPlatform.Application.Features.Scheduling;

public sealed record CreateLeaveRequest(Guid StudentId, DateOnly StartDate, DateOnly EndDate, string Reason)
    : IRequest<Guid>, IRequiresTenant;

public sealed class CreateLeaveRequestValidator : AbstractValidator<CreateLeaveRequest>
{
    public CreateLeaveRequestValidator()
    {
        RuleFor(x => x.StudentId).NotEmpty();
        RuleFor(x => x.EndDate).GreaterThanOrEqualTo(x => x.StartDate);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(1000);
    }
}

public sealed class CreateLeaveRequestHandler(IApplicationDbContext db, ICurrentUser current)
    : IRequestHandler<CreateLeaveRequest, Guid>
{
    public async Task<Guid> Handle(CreateLeaveRequest request, CancellationToken ct)
    {
        var leave = new LeaveRequest
        {
            TenantId = current.TenantId, StudentId = request.StudentId,
            StartDate = request.StartDate, EndDate = request.EndDate,
            Reason = request.Reason, RequestedBy = current.UserId ?? Guid.Empty
        };
        db.Set<LeaveRequest>().Add(leave);
        await db.SaveChangesAsync(ct);
        return leave.Id;
    }
}

public sealed record ReviewLeaveRequest(Guid LeaveRequestId, bool Approve, string? Note)
    : IRequest<bool>, IRequiresTenant;

public sealed class ReviewLeaveRequestHandler(IApplicationDbContext db, ICurrentUser current)
    : IRequestHandler<ReviewLeaveRequest, bool>
{
    public async Task<bool> Handle(ReviewLeaveRequest request, CancellationToken ct)
    {
        var leave = await db.Set<LeaveRequest>().FirstOrDefaultAsync(l => l.Id == request.LeaveRequestId, ct)
                    ?? throw new NotFoundException(nameof(LeaveRequest), request.LeaveRequestId);
        if (leave.Status != LeaveStatus.Pending) throw new ConflictException("Leave request was already reviewed.");
        leave.Status = request.Approve ? LeaveStatus.Approved : LeaveStatus.Rejected;
        leave.ReviewedBy = current.UserId;
        leave.ReviewedAt = DateTimeOffset.UtcNow;
        leave.ReviewNote = request.Note;
        await db.SaveChangesAsync(ct);
        return true;
    }
}
