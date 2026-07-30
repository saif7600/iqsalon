"use client";

import { FormEvent, useCallback, useEffect, useState } from "react";
import { Building2, CreditCard, Layers3, Plus, RefreshCw, ShieldCheck, Users, X } from "lucide-react";

type Overview = { tenants: number; activeTenants: number; plans: number; activeSubscriptions: number; pastDueSubscriptions: number };
type TenantRow = { id: string; name: string; slug: string; status: string; organizationCount: number; branchCount: number; userCount: number; subscriptionStatus?: string };
type PlanRow = { id: string; code: string; name: string; status: string; trialDays: number; prices: { currencyCode: string; billingInterval: string; amount: number }[] };
type SubscriptionRow = { id: string; tenantName?: string; organizationName?: string; planName?: string; status: string; currentPeriodEndUtc: string };
type ProvisionResult = { name: string; slug: string; email: string; invitationPath: string; subscriptionStatus: string };

export function PlatformAdminWorkspace() {
  const [overview, setOverview] = useState<Overview | null>(null);
  const [tenants, setTenants] = useState<TenantRow[]>([]);
  const [plans, setPlans] = useState<PlanRow[]>([]);
  const [subscriptions, setSubscriptions] = useState<SubscriptionRow[]>([]);
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(true);
  const [provisioning, setProvisioning] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [created, setCreated] = useState<ProvisionResult | null>(null);
  const load = useCallback(async () => {
    setLoading(true); setError("");
    try {
      const responses = await Promise.all(["overview", "tenants", "plans", "subscriptions"].map((path) => fetch(`/api/v1/platform/${path}`)));
      if (responses.some((response) => !response.ok)) throw new Error(responses.some((response) => response.status === 401 || response.status === 403) ? "Platform administrator access is required." : "Platform data could not be loaded.");
      const [overviewData, tenantData, planData, subscriptionData] = await Promise.all(responses.map((response) => response.json()));
      setOverview(overviewData); setTenants(tenantData); setPlans(planData); setSubscriptions(subscriptionData);
    } catch (reason) { setError(reason instanceof Error ? reason.message : "Platform data could not be loaded."); }
    finally { setLoading(false); }
  }, []);
  useEffect(() => { const timer = window.setTimeout(() => void load(), 0); return () => window.clearTimeout(timer); }, [load]);

  async function provision(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); setSubmitting(true); setError("");
    const data = new FormData(event.currentTarget);
    try {
      const response = await fetch("/api/v1/platform/tenants", {
        method: "POST", headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          tenantName: data.get("tenantName"), legalName: data.get("legalName"), tradingName: data.get("tradingName"),
          ownerName: data.get("ownerName"), ownerEmail: data.get("ownerEmail"), branchName: data.get("branchName"),
          branchCode: data.get("branchCode"), city: data.get("city"), countryCode: data.get("countryCode"),
          currencyCode: data.get("currencyCode"), language: data.get("language"), timeZone: data.get("timeZone"),
          planId: data.get("planId") || null, billingInterval: data.get("billingInterval"),
        }),
      });
      const payload = await response.json();
      if (!response.ok) throw new Error(payload.error ?? "Tenant provisioning failed.");
      setCreated(payload); setProvisioning(false); await load();
    } catch (reason) { setError(reason instanceof Error ? reason.message : "Tenant provisioning failed."); }
    finally { setSubmitting(false); }
  }

  if (error && !overview) return <section className="platform-access-state"><ShieldCheck size={30} /><h2>SaaS administration is isolated</h2><p>{error}</p></section>;
  return <div className="platform-workspace">
    <header className="platform-heading"><div><p className="eyebrow">AtiqSalon control plane</p><h1>SaaS Administration</h1><p>Provision tenants, assign commercial plans, and control subscription state.</p></div><div className="platform-heading-actions"><button className="button secondary" type="button" onClick={() => void load()} aria-label="Refresh platform data"><RefreshCw size={17} className={loading ? "spin" : ""} /></button><button className="button" type="button" onClick={() => { setCreated(null); setProvisioning(true); }}><Plus size={16} /> New tenant</button></div></header>
    {error && <div className="platform-alert" role="alert">{error}</div>}
    {created && <section className="platform-success"><div><strong>{created.name} was provisioned</strong><span>Owner: {created.email} · Subscription: {created.subscriptionStatus}</span></div><div><input readOnly value={`${window.location.origin}${created.invitationPath}`} aria-label="Owner invitation link" /><button className="button secondary" type="button" onClick={() => void navigator.clipboard.writeText(`${window.location.origin}${created.invitationPath}`)}>Copy invitation</button></div><small>This one-time invitation expires in 7 days. Send it securely to the tenant owner.</small></section>}
    <section className="platform-metrics"><Metric icon={Building2} label="Tenants" value={overview?.tenants ?? 0} detail={`${overview?.activeTenants ?? 0} active`} /><Metric icon={Layers3} label="Plans" value={overview?.plans ?? 0} detail="commercial catalogue" /><Metric icon={CreditCard} label="Subscriptions" value={overview?.activeSubscriptions ?? 0} detail={`${overview?.pastDueSubscriptions ?? 0} past due`} /><Metric icon={Users} label="Users" value={tenants.reduce((sum, row) => sum + row.userCount, 0)} detail="across all tenants" /></section>
    <section className="platform-grid">
      <article className="platform-panel platform-panel-wide"><Title name="Tenant directory" note="Cross-tenant operational view" count={tenants.length} /><div className="table-wrap"><table><thead><tr><th>Tenant</th><th>Status</th><th>Organizations</th><th>Branches</th><th>Users</th><th>Subscription</th></tr></thead><tbody>{tenants.map((row) => <tr key={row.id}><td><strong>{row.name}</strong><small>{row.slug}</small></td><td><Status value={row.status} /></td><td>{row.organizationCount}</td><td>{row.branchCount}</td><td>{row.userCount}</td><td><Status value={row.subscriptionStatus ?? "unassigned"} /></td></tr>)}</tbody></table></div></article>
      <article className="platform-panel"><Title name="Plan catalogue" note="Persisted commercial definitions" count={plans.length} /><div className="platform-list">{plans.length === 0 && <p className="empty-note">No SaaS plans have been configured.</p>}{plans.map((row) => <div className="platform-list-row" key={row.id}><div><strong>{row.name}</strong><small>{row.code} · {row.trialDays} trial days</small></div><div className="align-end"><Status value={row.status} /><small>{row.prices[0] ? `${row.prices[0].currencyCode} ${row.prices[0].amount}/${row.prices[0].billingInterval}` : "No price"}</small></div></div>)}</div></article>
      <article className="platform-panel"><Title name="Subscriptions" note="Current commercial lifecycle" count={subscriptions.length} /><div className="platform-list">{subscriptions.length === 0 && <p className="empty-note">No subscriptions have been activated.</p>}{subscriptions.slice(0, 8).map((row) => <div className="platform-list-row" key={row.id}><div><strong>{row.tenantName ?? "Unknown tenant"}</strong><small>{row.planName ?? "Unassigned plan"} · {row.organizationName}</small></div><div className="align-end"><Status value={row.status} /><small>to {new Date(row.currentPeriodEndUtc).toLocaleDateString()}</small></div></div>)}</div></article>
    </section>
    {provisioning && <div className="platform-modal-backdrop" role="presentation"><section className="platform-modal" role="dialog" aria-modal="true" aria-labelledby="provision-title"><header><div><span>Tenant provisioning</span><h2 id="provision-title">Create a new tenant</h2><p>Create the business, first branch, owner access, and subscription in one transaction.</p></div><button type="button" onClick={() => setProvisioning(false)} aria-label="Close"><X size={18} /></button></header><form onSubmit={provision}>
      <fieldset><legend>Business identity</legend><div className="platform-form-grid"><label>Tenant name *<input name="tenantName" required /></label><label>Trading name *<input name="tradingName" required /></label><label className="wide">Legal name *<input name="legalName" required /></label></div></fieldset>
      <fieldset><legend>Owner invitation</legend><div className="platform-form-grid"><label>Owner name *<input name="ownerName" autoComplete="name" required /></label><label>Owner email *<input name="ownerEmail" type="email" autoComplete="email" required /></label></div></fieldset>
      <fieldset><legend>Initial branch</legend><div className="platform-form-grid"><label>Branch name *<input name="branchName" required /></label><label>Branch code *<input name="branchCode" maxLength={20} required /></label><label>City<input name="city" /></label><label>Timezone *<select name="timeZone" defaultValue="Asia/Dubai"><option>Asia/Dubai</option><option>Asia/Riyadh</option><option>Asia/Qatar</option><option>Europe/London</option></select></label></div></fieldset>
      <fieldset><legend>Locale and subscription</legend><div className="platform-form-grid"><label>Country *<select name="countryCode" defaultValue="AE"><option value="AE">United Arab Emirates</option><option value="SA">Saudi Arabia</option><option value="QA">Qatar</option><option value="GB">United Kingdom</option></select></label><label>Currency *<select name="currencyCode" defaultValue="AED"><option>AED</option><option>SAR</option><option>QAR</option><option>GBP</option><option>USD</option></select></label><label>Language *<select name="language" defaultValue="en"><option value="en">English</option><option value="ar">Arabic</option></select></label><label>Plan<select name="planId"><option value="">No plan yet</option>{plans.filter((plan) => plan.status === "active").map((plan) => <option key={plan.id} value={plan.id}>{plan.name}</option>)}</select></label><label>Billing interval<select name="billingInterval" defaultValue="monthly"><option value="monthly">Monthly</option><option value="annual">Annual</option></select></label></div></fieldset>
      <footer><span>The owner receives organization-wide access after accepting the invitation.</span><div><button className="button secondary" type="button" onClick={() => setProvisioning(false)}>Cancel</button><button className="button" type="submit" disabled={submitting}>{submitting ? "Provisioning..." : "Provision tenant"}</button></div></footer>
    </form></section></div>}
  </div>;
}
function Metric({ icon: Icon, label, value, detail }: { icon: typeof Building2; label: string; value: number; detail: string }) { return <article className="platform-metric"><Icon size={18} /><div><span>{label}</span><strong>{value}</strong><small>{detail}</small></div></article>; }
function Title({ name, note, count }: { name: string; note: string; count: number }) { return <div className="panel-title"><div><span>{name}</span><small>{note}</small></div><b>{count}</b></div>; }
function Status({ value }: { value: string }) { return <span className={`status-pill status-${value.replace("_", "-")}`}>{value.replace("_", " ")}</span>; }
