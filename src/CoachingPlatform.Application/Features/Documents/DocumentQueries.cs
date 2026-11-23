using CoachingPlatform.Application.Common;
using CoachingPlatform.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CoachingPlatform.Application.Features.Documents;

public sealed record GenerateReportCard(Guid StudentId, Guid BatchId)
    : IRequest<byte[]>, IRequiresTenant;

public sealed class GenerateReportCardHandler(IApplicationDbContext db, IDocumentGenerator generator)
    : IRequestHandler<GenerateReportCard, byte[]>
{
    public async Task<byte[]> Handle(GenerateReportCard request, CancellationToken ct)
    {
        var student = await db.Set<Domain.Identity.User>().FirstOrDefaultAsync(u => u.Id == request.StudentId, ct)
                      ?? throw new NotFoundException(nameof(Domain.Identity.User), request.StudentId);
        var batch = await db.Set<Domain.Academics.Batch>().FirstOrDefaultAsync(b => b.Id == request.BatchId, ct)
                    ?? throw new NotFoundException("Batch", request.BatchId);
        var attendance = await db.Set<Domain.Reporting.AttendanceStat>()
            .Where(a => a.StudentId == request.StudentId && a.BatchId == request.BatchId).ToListAsync(ct);
        var grades = await db.Set<Domain.Reporting.GradeSummary>()
            .Where(g => g.StudentId == request.StudentId).ToListAsync(ct);
        var lines = grades.Select(g => new ReportCardLine($"Subject {g.SubjectId.ToString()[..8]}",
            g.MarksObtained, g.MaxMarks)).ToList();
        return generator.GenerateReportCard(new ReportCardData(
            student.FullName, batch.Name,
            attendance.Count == 0 ? 0m : Math.Round(attendance.Average(a => a.AttendancePercent), 2),
            grades.Count == 0 ? 0m : Math.Round(grades.Average(g => g.Average), 2), lines));
    }
}

public sealed record GenerateInvoicePdf(Guid InvoiceId) : IRequest<byte[]>, IRequiresTenant;

public sealed class GenerateInvoicePdfHandler(IApplicationDbContext db, IDocumentGenerator generator)
    : IRequestHandler<GenerateInvoicePdf, byte[]>
{
    public async Task<byte[]> Handle(GenerateInvoicePdf request, CancellationToken ct)
    {
        var invoice = await db.Set<Domain.Finance.Invoice>()
            .FirstOrDefaultAsync(i => i.Id == request.InvoiceId, ct)
            ?? throw new NotFoundException("Invoice", request.InvoiceId);
        var student = await db.Set<Domain.Identity.User>().FirstOrDefaultAsync(u => u.Id == invoice.StudentId, ct)
                      ?? throw new NotFoundException(nameof(Domain.Identity.User), invoice.StudentId);
        return generator.GenerateInvoice(new InvoiceDocumentData(
            invoice.InvoiceNumber, student.FullName, invoice.IssueDate, invoice.DueDate,
            invoice.SubTotal.Amount, invoice.Discount.Amount, invoice.Tax.Amount,
            invoice.Total.Amount, invoice.Total.Currency));
    }
}
