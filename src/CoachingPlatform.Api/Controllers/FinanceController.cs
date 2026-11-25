using CoachingPlatform.Application.DTOs.Finance;
using CoachingPlatform.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using CoachingPlatform.Domain.Common;
namespace CoachingPlatform.Api.Controllers;

[ApiController]
[Route("api")]
[Authorize(Roles = "Admin,Teacher")]
[Produces("application/json")]
public sealed class FinanceController(IFinanceService finance) : ControllerBase
{
    [HttpPost("invoices")]
    public async Task<IActionResult> CreateInvoice([FromBody] CreateInvoiceRequest request, CancellationToken ct)
        => Ok(await finance.CreateInvoiceAsync(request, ct));

    [HttpPost("invoices/{invoiceId:guid}/payments")]
    public async Task<IActionResult> RecordPayment([FromRoute] Guid invoiceId,
        [FromBody] RecordPaymentRequest request, CancellationToken ct)
        => Ok(await finance.RecordPaymentAsync(invoiceId, request, ct));

    [HttpPost("payments/{paymentId:guid}/refunds")]
    public async Task<IActionResult> Refund([FromRoute] Guid paymentId, [FromBody] RefundPaymentRequest request, CancellationToken ct)
        => Ok(await finance.RefundAsync(paymentId, request, ct));

    [HttpPost("fee-structures")]
    public async Task<IActionResult> CreateFeeStructure([FromBody] CreateFeeStructureRequest request, CancellationToken ct)
        => Ok(await finance.CreateFeeStructureAsync(request, ct));

    [HttpPost("fee-structures/{feeStructureId:guid}/installments")]
    public async Task<IActionResult> GenerateInstallments([FromRoute] Guid feeStructureId,
        [FromBody] GenerateInstallmentsRequest request, CancellationToken ct)
        => Ok(await finance.GenerateInstallmentsAsync(feeStructureId, request, ct));

    [HttpPost("discounts")]
    public async Task<IActionResult> CreateDiscount([FromBody] CreateDiscountRequest request, CancellationToken ct)
        => Ok(await finance.CreateDiscountAsync(request, ct));

    [HttpPost("scholarships")]
    public async Task<IActionResult> CreateScholarship([FromBody] CreateScholarshipRequest request, CancellationToken ct)
        => Ok(await finance.CreateScholarshipAsync(request, ct));

    [HttpGet("invoices/{invoiceId:guid}/pdf")]
    public async Task<IActionResult> InvoicePdf([FromRoute] Guid invoiceId, CancellationToken ct)
    {
        var bytes = await finance.GetInvoicePdfAsync(invoiceId, ct);
        return File(bytes, "application/pdf", $"invoice-{invoiceId}.pdf");
    }
}
