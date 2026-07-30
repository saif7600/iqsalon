"use client";

import { useEffect, useState } from "react";
import { apiRequest } from "@atiqsalon/sdk";
import { Card, ErrorState, LoadingState, PageTitle } from "@atiqsalon/ui";
import { PortalShell } from "./portal-shell";

type Branch = { id: string; name: string };
type Balance = { productId: string; product: string; quantityOnHand: number; quantityReserved: number; quantityAvailable: number; averageUnitCost: number; value: number };
type Report = { quantityOnHand: number; inventoryValue: number; lowStockProducts: number; wastageQuantity: number; approvedExpenses: number; postedReceipts: number; balances: Balance[] };
const money = (value: number) => new Intl.NumberFormat("en-AE", { style: "currency", currency: "AED" }).format(value);

export function InventoryWorkspace() {
  const [branches, setBranches] = useState<Branch[]>([]);
  const [branchId, setBranchId] = useState("");
  const [report, setReport] = useState<Report | null>(null);
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(true);
  async function load(id: string) {
    if (!id) return;
    setLoading(true); setError("");
    try { setReport(await apiRequest<Report>(`/reports/inventory?branchId=${id}`)); }
    catch { setError("Inventory operations could not be loaded for this branch."); }
    finally { setLoading(false); }
  }
  useEffect(() => { void (async () => {
    try { const items = await apiRequest<Branch[]>("/branches"); setBranches(items); setBranchId(items[0]?.id ?? ""); await load(items[0]?.id ?? ""); }
    catch { setError("Branches could not be loaded."); setLoading(false); }
  })(); }, []);
  return <PortalShell title="Inventory operations">
    <PageTitle eyebrow="Stock and purchasing" title="Inventory operations">Live balances, valuation, purchasing receipts, wastage, and operating expense evidence.</PageTitle>
    <label>Branch <select value={branchId} onChange={(e) => { setBranchId(e.target.value); void load(e.target.value); }}>{branches.map(x => <option key={x.id} value={x.id}>{x.name}</option>)}</select></label>
    {error && <ErrorState message={error} />}
    {loading ? <LoadingState /> : report && <>
      <div className="report-stats">
        <Card><small>Inventory value</small><strong>{money(report.inventoryValue)}</strong><span>{report.quantityOnHand} base units</span></Card>
        <Card><small>Low stock</small><strong>{report.lowStockProducts}</strong><span>At or below reorder point</span></Card>
        <Card><small>Wastage</small><strong>{report.wastageQuantity}</strong><span>Posted base units</span></Card>
        <Card><small>Approved expenses</small><strong>{money(report.approvedExpenses)}</strong><span>{report.postedReceipts} goods receipts</span></Card>
      </div>
      <Card><h2>Stock balances</h2>{report.balances.length === 0 ? <p className="muted">No stock has been posted.</p> :
        report.balances.map(x => <div className="report-row" key={`${x.productId}-${x.product}`}><span><strong>{x.product}</strong><small>{x.quantityAvailable} available · {x.quantityReserved} reserved</small></span><strong>{money(x.value)}</strong></div>)}</Card>
    </>}
  </PortalShell>;
}
