
public sealed class TenantClaimGuard(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated == true &&
            !Guid.TryParse(context.User.FindFirst("tenant_id")?.Value, out _))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { status = 401, message = "Invalid tenant claim." });
            return;
        }
        await next(context);
    }
}
