using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AtiqSalon.Api.Domain;
using Microsoft.IdentityModel.Tokens;
namespace AtiqSalon.Api.Security;

public sealed class TenantContext(IHttpContextAccessor accessor)
{
    public Guid? TenantId => accessor.HttpContext?.User.GetGuid("tenant_id");
    public Guid? UserId => accessor.HttpContext?.User.GetGuid("sub");
    public IReadOnlyCollection<Guid> BranchIds => accessor.HttpContext?.User.FindAll("branch_id").Select(x => Guid.TryParse(x.Value, out var id) ? id : Guid.Empty).Where(x => x != Guid.Empty).ToArray() ?? [];
    public bool HasOrganizationWideAccess => accessor.HttpContext?.User.IsInRole("OrganizationOwner") == true || accessor.HttpContext?.User.IsInRole("OrganizationAdmin") == true;
    public bool CanAccessBranch(Guid branchId) => HasOrganizationWideAccess || BranchIds.Contains(branchId);
    public bool HasPermission(string permission)
    {
        var user = accessor.HttpContext?.User;
        return user?.HasClaim("permission", permission) == true
            || user is not null && PermissionCatalog.ForRoles(user.FindAll(ClaimTypes.Role).Select(x => x.Value)).Contains(permission);
    }
    public bool IsPlatformContext => accessor.HttpContext?.User.HasClaim("platform_context", "true") == true;
}
public static class ClaimsExtensions
{
    public static Guid? GetGuid(this ClaimsPrincipal user, string type)
    {
        var raw = user.FindFirstValue(type);
        if (raw is null && type == "sub") raw = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(raw, out var value) ? value : null;
    }
}
public static class PermissionCatalog
{
    public static readonly string[] PlatformAll = ["platform.dashboard.read", "platform.tenants.read", "platform.tenants.manage", "platform.plans.read", "platform.plans.manage", "platform.subscriptions.read", "platform.subscriptions.manage", "platform.billing.read", "platform.usage.read", "platform.entitlements.read", "platform.provisioning.read", "platform.support.read", "platform.operations.read", "platform.security.read", "platform.releases.read"];
    public static readonly string[] AiAll = ["ai.use", "ai.reception.use", "ai.reception.manage", "ai.copilot.use", "ai.booking_optimizer.use", "ai.retention.read", "ai.retention.generate", "ai.revenue.read", "ai.revenue.generate", "ai.inventory.read", "ai.inventory.generate", "ai.staff_coach.self", "ai.staff_coach.manage", "ai.marketing.use", "ai.approvals.read", "ai.approvals.approve", "ai.approvals.execute", "ai.knowledge.read", "ai.knowledge.upload", "ai.knowledge.process", "ai.knowledge.archive", "ai.knowledge.restricted.read", "ai.settings.read", "ai.settings.update", "ai.provider.manage", "ai.routing.manage", "ai.prompts.read", "ai.prompts.manage", "ai.safety.read", "ai.safety.manage", "ai.usage.read", "ai.budget.manage", "ai.evaluations.read", "ai.evaluations.run", "ai.observability.read"];
    public static readonly string[] GrowthAll = ["iqai.use", "performance.read", "performance.manage", "loyalty.read", "loyalty.manage", "loyalty.adjust", "referrals.read", "referrals.manage"];
    public static readonly string[] WorkforceAll = ["workforce.settings.read", "workforce.settings.manage", "shifts.read", "shifts.manage", "shift_swaps.request", "shift_swaps.accept", "shift_swaps.approve", "attendance.read", "attendance.record", "attendance.correct", "attendance.approve", "leave.read", "leave.request", "leave.manage", "leave.approve", "payroll_inputs.read", "payroll_inputs.manage", "payroll_inputs.approve", "payroll_inputs.export"];
    public static readonly string[] InventoryAll = ["inventory.read", "inventory.manage", "inventory.movements.read", "inventory.adjust", "inventory.adjust.approve", "inventory.reconcile", "inventory.rebuild_balance", "inventory.negative_stock.override", "products.cost.read", "products.cost.update", "recipes.read", "recipes.create", "recipes.update", "recipes.activate", "consumption.read", "consumption.confirm", "consumption.adjust", "consumption.reverse", "suppliers.read", "suppliers.create", "suppliers.update", "suppliers.block", "purchase_requests.read", "purchase_requests.create", "purchase_requests.submit", "purchase_requests.approve", "purchase_requests.reject", "purchase_orders.read", "purchase_orders.create", "purchase_orders.update", "purchase_orders.submit", "purchase_orders.approve", "purchase_orders.send", "purchase_orders.cancel", "goods_receipts.read", "goods_receipts.create", "goods_receipts.post", "goods_receipts.reverse", "stock_transfers.read", "stock_transfers.create", "stock_transfers.request", "stock_transfers.approve", "stock_transfers.dispatch", "stock_transfers.receive", "stocktakes.read", "stocktakes.create", "stocktakes.count", "stocktakes.approve", "stocktakes.post", "wastage.read", "wastage.record", "wastage.approve", "expenses.read", "expenses.create", "expenses.submit", "expenses.approve", "expenses.mark_paid", "expenses.reverse", "petty_cash.read", "petty_cash.transact", "petty_cash.approve", "petty_cash.reconcile", "reports.inventory", "reports.purchasing", "reports.service_cost", "reports.expenses", "reports.operational_margin"];
    private static readonly IReadOnlyDictionary<string, string[]> InventoryRoles = new Dictionary<string, string[]>
    {
        ["BranchManager"] = ["inventory.read", "inventory.movements.read", "inventory.adjust.approve", "purchase_requests.read", "purchase_requests.approve", "stock_transfers.read", "stock_transfers.approve", "stocktakes.read", "stocktakes.approve", "wastage.read", "wastage.approve", "expenses.read", "expenses.approve", "reports.inventory"],
        ["Receptionist"] = ["inventory.read"],
        ["ServiceProvider"] = ["inventory.read", "recipes.read", "consumption.read", "consumption.confirm"],
        ["InventoryManager"] = ["inventory.read", "inventory.manage", "inventory.movements.read", "inventory.adjust", "inventory.reconcile", "products.cost.read", "products.cost.update", "recipes.read", "recipes.create", "recipes.update", "recipes.activate", "consumption.read", "consumption.adjust", "suppliers.read", "suppliers.create", "suppliers.update", "purchase_requests.read", "purchase_requests.create", "purchase_requests.submit", "purchase_orders.read", "purchase_orders.create", "purchase_orders.update", "purchase_orders.submit", "goods_receipts.read", "goods_receipts.create", "goods_receipts.post", "stock_transfers.read", "stock_transfers.create", "stock_transfers.request", "stock_transfers.dispatch", "stock_transfers.receive", "stocktakes.read", "stocktakes.create", "stocktakes.count", "stocktakes.post", "wastage.read", "wastage.record", "reports.inventory", "reports.purchasing"],
        ["Accountant"] = ["inventory.read", "inventory.movements.read", "products.cost.read", "suppliers.read", "purchase_orders.read", "goods_receipts.read", "expenses.read", "expenses.approve", "expenses.mark_paid", "petty_cash.read", "petty_cash.approve", "petty_cash.reconcile", "reports.inventory", "reports.purchasing", "reports.expenses", "reports.operational_margin"],
        ["Viewer"] = ["inventory.read"]
    };
    public static readonly string[] All = ["organization.read", "organization.update", "branch.read", "branch.create", "branch.update", "branch.delete", "user.read", "user.invite", "user.update", "role.read", "role.assign", "audit.read", "settings.read", "settings.update", "platform.tenants.read", "services.read", "services.create", "services.update", "services.activate", "services.deactivate", "staff.read", "staff.create", "staff.update", "staff.schedule.manage", "staff.capabilities.manage", "customers.read", "customers.create", "customers.update", "customers.notes.read", "customers.notes.sensitive.read", "customers.notes.create", "customers.consent.manage", "resources.read", "resources.create", "resources.update", "resources.activate", "resources.deactivate", "appointments.read", "appointments.create", "appointments.update", "appointments.confirm", "appointments.checkin", "appointments.start", "appointments.complete", "appointments.cancel", "appointments.mark_no_show", "appointments.reschedule", "booking_settings.read", "booking_settings.update", "tax.read", "tax.manage", "products.read", "products.create", "products.update", "pos.access", "pos.create_sale", "pos.hold_sale", "pos.resume_sale", "pos.post_sale", "pos.void_sale", "payments.read", "payments.record", "payments.allocate", "payments.cancel", "payments.manage", "refunds.read", "refunds.create", "deposits.read", "deposits.create", "deposits.apply", "packages.read", "packages.manage", "packages.sell", "packages.consume", "memberships.read", "memberships.manage", "memberships.sell", "memberships.renew", "memberships.consume", "gift_cards.read", "gift_cards.issue", "gift_cards.redeem", "commissions.read", "commissions.manage", "daily_closing.read", "daily_closing.create", "daily_closing.approve", "invoices.read", "invoices.issue", "invoices.send", "discounts.read", "discounts.apply", "discounts.approve", "tills.read", "tills.open", "tills.cash_in", "tills.cash_out", "tills.close", "reports.sales", "reports.tax", "reports.payments", "reports.cash"];
    public static readonly IReadOnlyDictionary<string, string[]> Roles = new Dictionary<string, string[]>
    {
        ["PlatformOwner"] = PlatformAll,
        ["PlatformAdministrator"] = PlatformAll,
        ["PlatformSuperAdmin"] = PlatformAll,
        ["PlatformBillingManager"] = ["platform.dashboard.read", "platform.tenants.read", "platform.plans.read", "platform.subscriptions.read", "platform.subscriptions.manage", "platform.billing.read", "platform.usage.read"],
        ["PlatformSupportAgent"] = ["platform.dashboard.read", "platform.tenants.read", "platform.plans.read", "platform.subscriptions.read", "platform.support.read", "platform.operations.read"],
        ["PlatformReadOnlyAuditor"] = ["platform.dashboard.read", "platform.tenants.read", "platform.plans.read", "platform.subscriptions.read", "platform.billing.read", "platform.usage.read", "platform.security.read", "platform.releases.read"],
        ["OrganizationOwner"] = All.Where(x => !x.StartsWith("platform.")).ToArray(),
        ["OrganizationAdmin"] = All.Where(x => !x.StartsWith("platform.") && x != "customers.notes.sensitive.read").ToArray(),
        ["BranchManager"] = ["organization.read", "branch.read", "branch.update", "services.read", "staff.read", "staff.schedule.manage", "customers.read", "customers.create", "customers.update", "appointments.read", "appointments.create", "appointments.update", "appointments.confirm", "appointments.checkin", "appointments.start", "appointments.complete", "appointments.cancel", "appointments.mark_no_show", "appointments.reschedule", "tax.read", "products.read", "pos.access", "pos.create_sale", "pos.post_sale", "pos.void_sale", "payments.read", "refunds.read", "refunds.create", "discounts.read", "discounts.apply", "discounts.approve", "tills.read", "tills.close", "reports.sales", "reports.payments", "reports.cash"],
        ["Receptionist"] = ["organization.read", "branch.read", "services.read", "staff.read", "customers.read", "customers.create", "customers.update", "customers.notes.read", "customers.notes.create", "resources.read", "appointments.read", "appointments.create", "appointments.update", "appointments.confirm", "appointments.checkin", "appointments.start", "appointments.complete", "appointments.cancel", "appointments.mark_no_show", "appointments.reschedule", "tax.read", "products.read", "pos.access", "pos.create_sale", "pos.hold_sale", "pos.resume_sale", "pos.post_sale", "payments.read", "payments.record", "payments.allocate", "invoices.read", "tills.read", "tills.open", "tills.close"],
        ["ServiceProvider"] = ["organization.read", "branch.read", "services.read", "staff.read", "customers.read", "appointments.read", "appointments.checkin", "appointments.start", "appointments.complete"],
        ["Cashier"] = ["organization.read", "branch.read", "services.read", "customers.read", "tax.read", "products.read", "pos.access", "pos.create_sale", "pos.hold_sale", "pos.resume_sale", "pos.post_sale", "payments.read", "payments.record", "payments.allocate", "refunds.read", "invoices.read", "tills.read", "tills.open", "tills.cash_in", "tills.cash_out", "tills.close"],
        ["InventoryManager"] = ["organization.read", "branch.read"],
        ["Accountant"] = ["organization.read", "branch.read", "audit.read", "tax.read", "tax.manage", "products.read", "pos.access", "payments.read", "payments.manage", "refunds.read", "deposits.read", "invoices.read", "discounts.read", "tills.read", "reports.sales", "reports.tax", "reports.payments", "reports.cash"],
        ["MarketingManager"] = ["organization.read", "branch.read"],
        ["Viewer"] = ["organization.read", "branch.read", "services.read", "staff.read", "customers.read", "resources.read", "appointments.read"]
    };
    public static string[] ForRoles(IEnumerable<string> roles)
    {
        var roleList = roles.ToArray();
        var inventory = roleList.Any(x => x is "OrganizationOwner" or "OrganizationAdmin")
            ? InventoryAll
            : roleList.SelectMany(role => InventoryRoles.GetValueOrDefault(role, [])).ToArray();
        var workforce = roleList.Any(x => x is "OrganizationOwner" or "OrganizationAdmin")
            ? WorkforceAll
            : roleList.Contains("BranchManager")
                ? ["workforce.settings.read", "shifts.read", "shifts.manage", "shift_swaps.request", "shift_swaps.accept", "shift_swaps.approve", "attendance.read", "attendance.record", "attendance.correct", "attendance.approve", "leave.read", "leave.request", "leave.approve", "payroll_inputs.read"]
                : roleList.Contains("ServiceProvider")
                    ? ["shifts.read", "shift_swaps.request", "shift_swaps.accept", "attendance.read", "attendance.record", "leave.read", "leave.request"]
                    : [];
        var growth = roleList.Any(x => x is "OrganizationOwner" or "OrganizationAdmin")
            ? GrowthAll
            : roleList.Contains("BranchManager")
                ? ["iqai.use", "performance.read", "performance.manage", "loyalty.read", "loyalty.adjust", "referrals.read", "referrals.manage"]
                : roleList.Contains("MarketingManager")
                    ? ["iqai.use", "loyalty.read", "loyalty.manage", "referrals.read", "referrals.manage"]
                    : roleList.Contains("ServiceProvider") ? ["iqai.use", "performance.read"] : [];
        var ai = roleList.Any(x => x is "OrganizationOwner" or "OrganizationAdmin")
            ? AiAll
            : roleList.Contains("BranchManager")
                ? ["ai.use", "ai.reception.use", "ai.reception.manage", "ai.copilot.use", "ai.booking_optimizer.use", "ai.retention.read", "ai.revenue.read", "ai.inventory.read", "ai.approvals.read", "ai.approvals.approve", "ai.usage.read"]
                : roleList.Contains("Receptionist") ? ["ai.use", "ai.reception.use"]
                : roleList.Contains("MarketingManager") ? ["ai.use", "ai.marketing.use", "ai.retention.read"]
                : roleList.Contains("InventoryManager") ? ["ai.use", "ai.inventory.read", "ai.inventory.generate"]
                : roleList.Contains("ServiceProvider") ? ["ai.use", "ai.staff_coach.self"] : [];
        return roleList.SelectMany(role => Roles.GetValueOrDefault(role, [])).Concat(inventory).Concat(workforce).Concat(growth).Concat(ai).Distinct().ToArray();
    }
}
public static class TokenFactory
{
    public static string Create(User user, IEnumerable<string> permissions, IEnumerable<Guid> branchIds, string signingKey)
    {
        var claims = new List<Claim> { new(JwtRegisteredClaimNames.Sub, user.Id.ToString()), new("tenant_id", user.TenantId.ToString()), new(JwtRegisteredClaimNames.Email, user.Email) };
        claims.AddRange(user.Roles.Select(x => new Claim(ClaimTypes.Role, x)));
        if (user.Roles.Any(x => x.StartsWith("Platform", StringComparison.Ordinal))) claims.Add(new Claim("platform_context", "true"));
        claims.AddRange(branchIds.Distinct().Select(x => new Claim("branch_id", x.ToString())));
        var token = new JwtSecurityToken("atiqsalon-api", "atiqsalon-portal", claims, expires: DateTime.UtcNow.AddMinutes(15), signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)), SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
