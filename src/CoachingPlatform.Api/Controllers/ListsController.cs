using CoachingPlatform.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoachingPlatform.Api.Controllers;

/// <summary>List views for the operations console. All reads are tenant-scoped by the DbContext global filters.</summary>
[ApiController]
[Route("api")]
[Authorize(Roles = "Admin,Teacher")]
[Produces("application/json")]
public sealed class ListsController(IListService lists) : ControllerBase
{
    [HttpGet("academic-years")]
    public async Task<IActionResult> AcademicYears(CancellationToken ct) => Ok(await lists.AcademicYearsAsync(ct));

    [HttpPost("academic-years")]
    public async Task<IActionResult> CreateAcademicYear(
        [FromBody] AcademicYearRequest request, CancellationToken ct)
        => Ok(new { id = await lists.CreateAcademicYearAsync(request.Name, request.StartDate, request.EndDate, ct) });

    [HttpGet("courses")]
    public async Task<IActionResult> Courses(CancellationToken ct) => Ok(await lists.CoursesAsync(ct));

    [HttpGet("schedule-rules")]
    public async Task<IActionResult> ScheduleRules(CancellationToken ct) => Ok(await lists.ScheduleRulesAsync(ct));

    [HttpGet("scheduling/leave-requests")]
    public async Task<IActionResult> LeaveRequests(CancellationToken ct) => Ok(await lists.LeaveRequestsAsync(ct));

    [HttpGet("assessments")]
    public async Task<IActionResult> Assessments(CancellationToken ct) => Ok(await lists.AssessmentsAsync(ct));

    [HttpGet("fee-structures")]
    public async Task<IActionResult> FeeStructures(CancellationToken ct) => Ok(await lists.FeeStructuresAsync(ct));

    [HttpGet("invoices")]
    public async Task<IActionResult> Invoices(CancellationToken ct) => Ok(await lists.InvoicesAsync(ct));

    [HttpGet("discounts")]
    public async Task<IActionResult> Discounts(CancellationToken ct) => Ok(await lists.DiscountsAsync(ct));

    [HttpGet("scholarships")]
    public async Task<IActionResult> Scholarships(CancellationToken ct) => Ok(await lists.ScholarshipsAsync(ct));

    [HttpGet("crm/follow-ups")]
    public async Task<IActionResult> FollowUps(CancellationToken ct) => Ok(await lists.FollowUpsAsync(ct));
}

public sealed record AcademicYearRequest(string Name, DateOnly StartDate, DateOnly EndDate);
