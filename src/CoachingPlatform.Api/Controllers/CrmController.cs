using CoachingPlatform.Application.DTOs.Crm;
using CoachingPlatform.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoachingPlatform.Api.Controllers;

[ApiController]
[Route("api/crm")]
[Authorize]
[Produces("application/json")]
public sealed class CrmController(ICrmService crm) : ControllerBase
{
    [HttpPost("leads")]
    public async Task<IActionResult> CreateLead([FromBody] CreateLeadRequest r, CancellationToken ct)
        => Ok(new { id = await crm.CreateLeadAsync(r, ct) });
    [HttpPost("leads/{id:guid}/status")]
    public async Task<IActionResult> LeadStatus([FromRoute] Guid id, [FromBody] UpdateLeadStatusRequest r, CancellationToken ct)
        => Ok(new { updated = await crm.UpdateLeadStatusAsync(id, r, ct) });
    [HttpGet("leads")]
    public async Task<IActionResult> Leads([FromQuery] Domain.Common.LeadStatus? status, CancellationToken ct)
        => Ok(await crm.ListLeadsAsync(status, ct));

    [HttpPost("applications")]
    public async Task<IActionResult> CreateApplication([FromBody] CreateApplicationRequest r, CancellationToken ct)
        => Ok(new { id = await crm.CreateApplicationAsync(r, ct) });
    [HttpPost("applications/{id:guid}/documents")]
    public async Task<IActionResult> AddDocument([FromRoute] Guid id, [FromBody] AddApplicationDocumentRequest r, CancellationToken ct)
        => Ok(new { id = await crm.AddDocumentAsync(id, r, ct) });
    [HttpPost("applications/{id:guid}/review")]
    public async Task<IActionResult> Review([FromRoute] Guid id, [FromBody] ReviewApplicationRequest r, CancellationToken ct)
        => Ok(new { updated = await crm.ReviewApplicationAsync(id, r, ct) });
    [HttpPost("applications/{id:guid}/enroll")]
    public async Task<IActionResult> Enroll([FromRoute] Guid id, [FromBody] EnrollApplicantRequest r, CancellationToken ct)
        => Ok(new { userId = await crm.EnrollApplicantAsync(id, r, ct) });
    [HttpGet("applications")]
    public async Task<IActionResult> Applications([FromQuery] Guid? courseId,
        [FromQuery] Domain.Common.ApplicationStatus? status, CancellationToken ct)
        => Ok(await crm.ListApplicationsAsync(courseId, status, ct));

    [HttpPost("follow-ups")]
    public async Task<IActionResult> CreateFollowUp([FromBody] CreateFollowUpRequest r, CancellationToken ct)
        => Ok(new { id = await crm.CreateFollowUpAsync(r, ct) });
    [HttpPost("follow-ups/{id:guid}/complete")]
    public async Task<IActionResult> CompleteFollowUp([FromRoute] Guid id, [FromQuery] string? note, CancellationToken ct)
        => Ok(new { completed = await crm.CompleteFollowUpAsync(id, note, ct) });
}
