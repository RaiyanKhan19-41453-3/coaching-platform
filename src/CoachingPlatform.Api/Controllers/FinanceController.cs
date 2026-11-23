using CoachingPlatform.Application.DTOs.Finance;
using CoachingPlatform.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoachingPlatform.Api.Controllers;

[ApiController]
[Route("api")]
[Authorize]
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
}
