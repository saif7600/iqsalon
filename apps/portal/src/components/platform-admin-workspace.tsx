"use client";
import { useCallback, useEffect, useState } from "react";
import { Building2, CreditCard, Layers3, RefreshCw, ShieldCheck, Users } from "lucide-react";

type Overview = { tenants: number; activeTenants: number; plans: number; activeSubscriptions: number; pastDueSubscriptions: number };
type TenantRow = { id: string; name: string; slug: string; status: string; organizationCount: number; branchCount: number; userCount: number; subscriptionStatus?: string };
type PlanRow = { id: string; code: string; name: string; status: string; trialDays: number; prices: { currencyCode: string; billingInterval: string; amount: number }[] };
type SubscriptionRow = { id: string; tenantName?: string; organizationName?: string; planName?: string; status: string; currentPeriodEndUtc: string };

export function PlatformAdminWorkspace() {
  const [overview, setOverview] = useState<Overview | null>(null);
  const [tenants, setTenants] = useState<TenantRow[]>([]);
  const [plans, setPlans] = useState<PlanRow[]>([]);
  const [subscriptions, setSubscriptions] = useState<SubscriptionRow[]>([]);
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(true);
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
  useEffect(() => {
    const initialLoad = window.setTimeout(() => void load(), 0);
    return () => window.clearTimeout(initialLoad);
  }, [load]);
  if (error) return <section className="platform-access-state"><ShieldCheck size={30} /><h2>SaaS administration is isolated</h2><p>{error}</p></section>;
  return <div className="platform-workspace">
    <header className="platform-heading"><div><p className="eyebrow">AtiqSalon control plane</p><h1>SaaS Administration</h1><p>Tenant operations, commercial plans, and subscription state in one controlled workspace.</p></div><button className="button secondary" type="button" onClick={() => void load()} aria-label="Refresh platform data"><RefreshCw size={17} className={loading ? "spin" : ""} /></button></header>
    <section className="platform-metrics"><Metric icon={Building2} label="Tenants" value={overview?.tenants ?? 0} detail={`${overview?.activeTenants ?? 0} active`} /><Metric icon={Layers3} label="Plans" value={overview?.plans ?? 0} detail="commercial catalogue" /><Metric icon={CreditCard} label="Subscriptions" value={overview?.activeSubscriptions ?? 0} detail={`${overview?.pastDueSubscriptions ?? 0} past due`} /><Metric icon={Users} label="Users" value={tenants.reduce((sum, row) => sum + row.userCount, 0)} detail="across all tenants" /></section>
    <section className="platform-grid">
      <article className="platform-panel platform-panel-wide"><Title name="Tenant directory" note="Cross-tenant operational view" count={tenants.length} /><div className="table-wrap"><table><thead><tr><th>Tenant</th><th>Status</th><th>Organizations</th><th>Branches</th><th>Users</th><th>Subscription</th></tr></thead><tbody>{tenants.map((row) => <tr key={row.id}><td><strong>{row.name}</strong><small>{row.slug}</small></td><td><Status value={row.status} /></td><td>{row.organizationCount}</td><td>{row.branchCount}</td><td>{row.userCount}</td><td><Status value={row.subscriptionStatus ?? "unassigned"} /></td></tr>)}</tbody></table></div></article>
      <article className="platform-panel"><Title name="Plan catalogue" note="Persisted commercial definitions" count={plans.length} /><div className="platform-list">{plans.length === 0 && <p className="empty-note">No SaaS plans have been configured.</p>}{plans.map((row) => <div className="platform-list-row" key={row.id}><div><strong>{row.name}</strong><small>{row.code} · {row.trialDays} trial days</small></div><div className="align-end"><Status value={row.status} /><small>{row.prices[0] ? `${row.prices[0].currencyCode} ${row.prices[0].amount}/${row.prices[0].billingInterval}` : "No price"}</small></div></div>)}</div></article>
      <article className="platform-panel"><Title name="Subscriptions" note="Current commercial lifecycle" count={subscriptions.length} /><div className="platform-list">{subscriptions.length === 0 && <p className="empty-note">No subscriptions have been activated.</p>}{subscriptions.slice(0, 8).map((row) => <div className="platform-list-row" key={row.id}><div><strong>{row.tenantName ?? "Unknown tenant"}</strong><small>{row.planName ?? "Unassigned plan"} · {row.organizationName}</small></div><div className="align-end"><Status value={row.status} /><small>to {new Date(row.currentPeriodEndUtc).toLocaleDateString()}</small></div></div>)}</div></article>
    </section>
  </div>;
}
function Metric({ icon: Icon, label, value, detail }: { icon: typeof Building2; label: string; value: number; detail: string }) { return <article className="platform-metric"><Icon size={18} /><div><span>{label}</span><strong>{value}</strong><small>{detail}</small></div></article>; }
function Title({ name, note, count }: { name: string; note: string; count: number }) { return <div className="panel-title"><div><span>{name}</span><small>{note}</small></div><b>{count}</b></div>; }
function Status({ value }: { value: string }) { return <span className={`status-pill status-${value.replace("_", "-")}`}>{value.replace("_", " ")}</span>; }
