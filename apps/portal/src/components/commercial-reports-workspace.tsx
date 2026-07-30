"use client";

import { FormEvent, useCallback, useEffect, useState } from "react";
import { apiRequest } from "@atiqsalon/sdk";
import { Badge, Button, Card, ErrorState, LoadingState, PageTitle } from "@atiqsalon/ui";
import { PortalShell } from "./portal-shell";

type Branch = { id: string; name: string };
type Report = {
  branch: { id: string; name: string; timeZone: string };
  period: { from: string; to: string };
  sales: { count: number; gross: number; discounts: number; tax: number; tips: number; net: number };
  refunds: { count: number; amount: number };
  payments: Array<{ paymentMethodId: string; name: string; type: string; inbound: number; outbound: number }>;
  vat: Array<{ code: string; rate: number; taxable: number; tax: number }>;
  commissions: { earned: number; reversed: number; net: number };
  liabilities: { customerDeposits: number; giftCards: number; activePackages: number; activeMemberships: number };
  closings: Array<{ id: string; businessDate: string; status: string; netSales: number; paymentsIn: number; refundsOut: number; cashVariance: number }>;
};

const today = new Date().toISOString().slice(0, 10);
const monthStart = `${today.slice(0, 8)}01`;
const money = (value: number) => new Intl.NumberFormat("en-AE", {
  style: "currency", currency: "AED",
}).format(value);

export function CommercialReportsWorkspace() {
  const [branches, setBranches] = useState<Branch[]>([]);
  const [branchId, setBranchId] = useState("");
  const [from, setFrom] = useState(monthStart);
  const [to, setTo] = useState(today);
  const [report, setReport] = useState<Report | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  const load = useCallback(async (selectedBranch: string, selectedFrom: string, selectedTo: string) => {
    if (!selectedBranch) return;
    setLoading(true);
    setError("");
    try {
      const query = new URLSearchParams({ branchId: selectedBranch, from: selectedFrom, to: selectedTo });
      setReport(await apiRequest<Report>(`/reports/commercial?${query}`));
    } catch {
      setError("The commercial report could not be loaded for this branch and period.");
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    async function initialize() {
      try {
        const available = await apiRequest<Branch[]>("/branches");
        setBranches(available);
        const first = available[0]?.id ?? "";
        setBranchId(first);
        await load(first, monthStart, today);
      } catch {
        setError("Branches or commercial reports could not be loaded.");
        setLoading(false);
      }
    }
    void initialize();
  }, [load]);

  function submit(event: FormEvent) {
    event.preventDefault();
    void load(branchId, from, to);
  }

  return (
    <PortalShell title="Commercial reports">
      <PageTitle eyebrow="Reconciled ledgers" title="Commercial reports">
        Sales, VAT, payments, liabilities, commissions, and daily closing evidence from persisted records.
      </PageTitle>
      <form className="report-filters" onSubmit={submit}>
        <label>Branch<select value={branchId} onChange={(event) => setBranchId(event.target.value)}>
          {branches.map((branch) => <option key={branch.id} value={branch.id}>{branch.name}</option>)}
        </select></label>
        <label>From<input type="date" value={from} onChange={(event) => setFrom(event.target.value)} /></label>
        <label>To<input type="date" value={to} onChange={(event) => setTo(event.target.value)} /></label>
        <Button type="submit">Run report</Button>
      </form>
      {error && <ErrorState message={error} />}
      {loading ? <LoadingState /> : report && (
        <>
          <div className="report-stats">
            <Card><small>Net sales</small><strong>{money(report.sales.net)}</strong><span>{report.sales.count} posted sales</span></Card>
            <Card><small>VAT</small><strong>{money(report.sales.tax)}</strong><span>{money(report.sales.discounts)} discounts</span></Card>
            <Card><small>Refunds</small><strong>{money(report.refunds.amount)}</strong><span>{report.refunds.count} refunds</span></Card>
            <Card><small>Commission</small><strong>{money(report.commissions.net)}</strong><span>{money(report.commissions.reversed)} reversed</span></Card>
          </div>
          <div className="report-grid">
            <Card><h2>Payment methods</h2>{report.payments.length === 0 ? <p className="muted">No payment movement.</p> :
              report.payments.map((item) => <div className="report-row" key={item.paymentMethodId}><span>{item.name}<small>{item.type}</small></span><strong>{money(item.inbound - item.outbound)}</strong></div>)}</Card>
            <Card><h2>VAT summary</h2>{report.vat.length === 0 ? <p className="muted">No taxable sales.</p> :
              report.vat.map((item) => <div className="report-row" key={`${item.code}-${item.rate}`}><span>{item.code}<small>{item.rate}% on {money(item.taxable)}</small></span><strong>{money(item.tax)}</strong></div>)}</Card>
            <Card><h2>Stored-value liabilities</h2>
              <div className="report-row"><span>Customer deposits</span><strong>{money(report.liabilities.customerDeposits)}</strong></div>
              <div className="report-row"><span>Gift cards</span><strong>{money(report.liabilities.giftCards)}</strong></div>
              <div className="report-row"><span>Active packages</span><strong>{report.liabilities.activePackages}</strong></div>
              <div className="report-row"><span>Active memberships</span><strong>{report.liabilities.activeMemberships}</strong></div>
            </Card>
            <Card><h2>Daily closings</h2>{report.closings.length === 0 ? <p className="muted">No closings in this period.</p> :
              report.closings.map((closing) => <div className="report-row" key={closing.id}><span>{closing.businessDate}<Badge>{closing.status}</Badge></span><strong>{money(closing.netSales)}</strong></div>)}</Card>
          </div>
        </>
      )}
    </PortalShell>
  );
}
