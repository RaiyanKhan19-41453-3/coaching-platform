using CoachingPlatform.Application.Common;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CoachingPlatform.Infrastructure.Documents;

public sealed class QuestPdfDocumentGenerator : IDocumentGenerator
{
    public QuestPdfDocumentGenerator() => QuestPDF.Settings.License = LicenseType.Community;

    public byte[] GenerateReportCard(ReportCardData data) => Document.Create(container =>
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(2, Unit.Centimetre);
            page.Header().Text("Report Card").FontSize(20).Bold();
            page.Content().Column(column =>
            {
                column.Item().Text($"Student: {data.StudentName}");
                column.Item().Text($"Course: {data.CourseName}");
                column.Item().Text($"Attendance: {data.AttendancePercent:0.##}%   Average: {data.AverageMarks:0.##}%");
                column.Item().PaddingTop(10).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(3); columns.RelativeColumn(); columns.RelativeColumn();
                    });
                    table.Header(header =>
                    {
                        header.Cell().Text("Subject").Bold();
                        header.Cell().Text("Obtained").Bold();
                        header.Cell().Text("Maximum").Bold();
                    });
                    foreach (var line in data.Lines)
                    {
                        table.Cell().Text(line.Subject);
                        table.Cell().Text(line.Obtained.ToString("0.##"));
                        table.Cell().Text(line.Maximum.ToString("0.##"));
                    }
                });
            });
            page.Footer().AlignRight().Text(x =>
            {
                x.Span("Generated "); x.Span(DateTime.UtcNow.ToString("yyyy-MM-dd"));
            });
        });
    }).GeneratePdf();

    public byte[] GenerateInvoice(InvoiceDocumentData data) => Document.Create(container =>
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(2, Unit.Centimetre);
            page.Header().Text("Invoice").FontSize(20).Bold();
            page.Content().Column(column =>
            {
                column.Item().Text($"Invoice: {data.InvoiceNumber}");
                column.Item().Text($"Student: {data.StudentName}");
                column.Item().Text($"Issued: {data.IssueDate}   Due: {data.DueDate}");
                column.Item().PaddingTop(10).Text($"Subtotal: {data.SubTotal:N2} {data.Currency}");
                column.Item().Text($"Discount: {data.Discount:N2} {data.Currency}");
                column.Item().Text($"Tax: {data.Tax:N2} {data.Currency}");
                column.Item().Text($"Total: {data.Total:N2} {data.Currency}").Bold();
            });
        });
    }).GeneratePdf();
}
