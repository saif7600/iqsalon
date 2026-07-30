using AtiqSalon.Api.Data;
using AtiqSalon.Api.Security;
using Microsoft.EntityFrameworkCore;

namespace AtiqSalon.Api.Application;

public static class CommercialCompletionEndpoints
{
    public static IEndpointRouteBuilder MapCommercialCompletionApi(this IEndpointRouteBuilder endpoints)
    {
        var api = endpoints.MapGroup("/api/v1");
        api.MapPost("/discount-approvals/{id:guid}/apply", async (Guid id,
            CommercialCompletionService service, CancellationToken ct) =>
        {
            var result = await service.ApplyDiscount(id, ct);
            return result.IsSuccess ? Results.Ok(result) : Results.Conflict(result);
        }).RequireAuthorization("discounts.apply");
        api.MapGet("/sales/{id:guid}/discount-approvals", async (Guid id,
            TenantContext tenant, AppDbContext db, CancellationToken ct) =>
        {
            var sale = await db.Sales.SingleOrDefaultAsync(x => x.Id == id, ct);
            return sale is null || !tenant.CanAccessBranch(sale.BranchId)
                ? Results.NotFound()
                : Results.Ok(await db.DiscountApprovalRequests.Where(x => x.SaleId == id)
                    .OrderByDescending(x => x.CreatedAtUtc).ToListAsync(ct));
        }).RequireAuthorization("discounts.read");
        api.MapPost("/till-sessions/{id:guid}/cash-movements", async (Guid id,
            CashMovementRequest request, CommercialCompletionService service, CancellationToken ct) =>
        {
            var result = await service.RecordCashMovement(id, request, ct);
            return result.IsSuccess ? Results.Ok(result) : Results.Conflict(result);
        }).RequireAuthorization("tills.cash_in");
        api.MapPost("/till-sessions/{id:guid}/approve-variance", async (Guid id,
            ApproveTillVarianceRequest request, CommercialCompletionService service, CancellationToken ct) =>
        {
            var result = await service.ApproveTillVariance(id, request, ct);
            return result.IsSuccess ? Results.Ok(result) : Results.Conflict(result);
        }).RequireAuthorization("tills.close");
        api.MapGet("/sales/{id:guid}/financial-documents", async (Guid id, TenantContext tenant,
            AppDbContext db, CancellationToken ct) =>
        {
            var sale = await db.Sales.SingleOrDefaultAsync(x => x.Id == id, ct);
            if (sale is null || !tenant.CanAccessBranch(sale.BranchId)) return Results.NotFound();
            var invoice = await db.Invoices.SingleOrDefaultAsync(x => x.SaleId == id, ct);
            var creditNotes = await db.CreditNotes.Where(x => x.SaleId == id)
                .OrderBy(x => x.IssuedAtUtc).ToListAsync(ct);
            var refunds = await db.Refunds.Where(x => x.SaleId == id)
                .OrderBy(x => x.RefundedAtUtc).ToListAsync(ct);
            var lines = await db.SaleLines.Where(x => x.SaleId == id)
                .OrderBy(x => x.Sequence).ToListAsync(ct);
            var payments = await db.PaymentAllocations.Where(x => x.SaleId == id)
                .Join(db.Payments, x => x.PaymentId, x => x.Id,
                    (allocation, payment) => new { payment, allocation.Amount }).ToListAsync(ct);
            return Results.Ok(new { sale, invoice, creditNotes, refunds, lines, payments });
        }).RequireAuthorization("invoices.read");
        return endpoints;
    }
}
