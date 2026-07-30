"use client";

import { FormEvent, useCallback, useEffect, useState } from "react";
import { apiRequest } from "@atiqsalon/sdk";
import { Badge, Button, Card, ErrorState, LoadingState, PageTitle } from "@atiqsalon/ui";
import { PortalShell } from "./portal-shell";

type Sale = {
  id: string; branchId: string; customerId?: string; saleNumber: string; status: string;
  currencyCode: string; businessDate: string; subtotal: number; discountTotal: number;
  taxTotal: number; grandTotal: number; paidTotal: number; balanceDue: number;
};
type Line = { id: string; descriptionSnapshot: string; quantity: number; unitPrice: number; taxAmount: number; lineTotal: number };
type Detail = { sale: Sale; lines: Line[]; payments: Array<{ payment: { id: string; paymentNumber: string; amount: number }; amount: number }>; invoice?: { invoiceNumber: string } };
type Named = { id: string; name: string; code: string };
type Deposit = { id: string; depositNumber: string; availableAmount: number };

const money = (value: number, currency = "AED") => new Intl.NumberFormat("en-AE", {
  style: "currency", currency,
}).format(value);

export function SaleOperationsWorkspace({ saleId }: { saleId?: string }) {
  const [sales, setSales] = useState<Sale[]>([]);
  const [detail, setDetail] = useState<Detail | null>(null);
  const [methods, setMethods] = useState<Named[]>([]);
  const [packages, setPackages] = useState<Named[]>([]);
  const [memberships, setMemberships] = useState<Named[]>([]);
  const [deposits, setDeposits] = useState<Deposit[]>([]);
  const [loading, setLoading] = useState(true);
  const [working, setWorking] = useState("");
  const [error, setError] = useState("");
  const [message, setMessage] = useState("");

  const loadDetail = useCallback(async (id: string) => {
    const result = await apiRequest<Detail>(`/sales/${id}`);
    setDetail(result);
    if (result.sale.customerId) {
      setDeposits(await apiRequest<Deposit[]>(`/deposits?customerId=${result.sale.customerId}`));
    } else {
      setDeposits([]);
    }
  }, []);

  useEffect(() => {
    async function load() {
      try {
        if (saleId) {
          const [, paymentMethods, packageRows, membershipRows] = await Promise.all([
            loadDetail(saleId),
            apiRequest<Named[]>("/payment-methods"),
            apiRequest<Named[]>("/packages"),
            apiRequest<Named[]>("/membership-plans"),
          ]);
          setMethods(paymentMethods);
          setPackages(packageRows);
          setMemberships(membershipRows);
        } else {
          setSales(await apiRequest<Sale[]>("/sales"));
        }
      } catch {
        setError("Sale operations could not be loaded for this account.");
      } finally {
        setLoading(false);
      }
    }
    void load();
  }, [loadDetail, saleId]);

  async function mutate(label: string, action: () => Promise<unknown>) {
    if (!saleId) return;
    setWorking(label);
    setError("");
    setMessage("");
    try {
      await action();
      await loadDetail(saleId);
      setMessage(`${label} completed.`);
    } catch {
      setError(`${label} was rejected by the commercial controls.`);
    } finally {
      setWorking("");
    }
  }

  function refund(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!detail) return;
    const data = new FormData(event.currentTarget);
    void mutate("Refund", () => apiRequest(`/sales/${detail.sale.id}/refunds`, {
      method: "POST",
      body: JSON.stringify({
        paymentMethodId: data.get("paymentMethodId"),
        amount: Number(data.get("amount")),
        reason: data.get("reason"),
        reference: data.get("reference") || null,
        idempotencyKey: crypto.randomUUID(),
        tillSessionId: null,
      }),
    }));
  }

  function applyDeposit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!detail) return;
    const data = new FormData(event.currentTarget);
    const depositId = String(data.get("depositId"));
    void mutate("Deposit application", () => apiRequest(`/deposits/${depositId}/apply`, {
      method: "POST",
      body: JSON.stringify({
        saleId: detail.sale.id,
        amount: Number(data.get("amount")),
        applicationId: crypto.randomUUID(),
      }),
    }));
  }

  function redeemGift(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!detail) return;
    const data = new FormData(event.currentTarget);
    void mutate("Gift-card redemption", () => apiRequest("/gift-cards/redeem", {
      method: "POST",
      body: JSON.stringify({
        code: data.get("code"),
        saleId: detail.sale.id,
        amount: Number(data.get("amount")),
        idempotencyKey: crypto.randomUUID(),
      }),
    }));
  }

  function activatePackage(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!detail?.sale.customerId) return;
    const data = new FormData(event.currentTarget);
    void mutate("Package activation", () => apiRequest(`/packages/${data.get("packageId")}/activate`, {
      method: "POST",
      body: JSON.stringify({
        branchId: detail.sale.branchId,
        customerId: detail.sale.customerId,
        saleId: detail.sale.id,
      }),
    }));
  }

  function enrollMembership(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!detail?.sale.customerId) return;
    const data = new FormData(event.currentTarget);
    void mutate("Membership enrollment", () => apiRequest(`/membership-plans/${data.get("planId")}/enroll`, {
      method: "POST",
      body: JSON.stringify({
        branchId: detail.sale.branchId,
        customerId: detail.sale.customerId,
        saleId: detail.sale.id,
      }),
    }));
  }

  return (
    <PortalShell title={saleId ? "Sale detail" : "Sale history"}>
      <PageTitle eyebrow="Commercial operations" title={saleId ? "Sale detail" : "Sale history"}>
        Posted and in-progress sales with settlement and stored-value operations.
      </PageTitle>
      {error && <ErrorState message={error} />}
      {message && <p className="notice">{message}</p>}
      {loading ? <LoadingState /> : !saleId ? (
        <div className="records-table">
          {sales.length === 0 ? <p className="muted sale-empty">No sales have been recorded.</p> : sales.map((sale) => (
            <a className="sale-row" href={`/pos/sales/${sale.id}`} key={sale.id}>
              <span><strong>{sale.saleNumber}</strong><small>{sale.businessDate}</small></span>
              <span>{money(sale.grandTotal, sale.currencyCode)}</span>
              <Badge>{sale.status}</Badge>
            </a>
          ))}
        </div>
      ) : detail && (
        <>
          <div className="sale-summary">
            <Card><small>Sale</small><strong>{detail.sale.saleNumber}</strong><Badge>{detail.sale.status}</Badge></Card>
            <Card><small>Total</small><strong>{money(detail.sale.grandTotal, detail.sale.currencyCode)}</strong><span>VAT {money(detail.sale.taxTotal)}</span></Card>
            <Card><small>Paid</small><strong>{money(detail.sale.paidTotal, detail.sale.currencyCode)}</strong><span>Balance {money(detail.sale.balanceDue)}</span></Card>
            <Card><small>Invoice</small><strong>{detail.invoice?.invoiceNumber ?? "Not issued"}</strong><span>{detail.sale.businessDate}</span></Card>
          </div>
          <div className="actions sale-actions"><a className="button secondary" href={`/pos/sales/${detail.sale.id}/print`}>Print documents</a></div>
          <div className="sale-ops-grid">
            <Card><h2>Lines</h2>{detail.lines.map((line) => <div className="report-row" key={line.id}><span>{line.descriptionSnapshot}<small>{line.quantity} x {money(line.unitPrice)}</small></span><strong>{money(line.lineTotal)}</strong></div>)}</Card>
            <Card><h2>Payments</h2>{detail.payments.length === 0 ? <p className="muted">No payments recorded.</p> : detail.payments.map((item) => <div className="report-row" key={item.payment.id}><span>{item.payment.paymentNumber}</span><strong>{money(item.amount)}</strong></div>)}</Card>
            {detail.sale.status === "Posted" && <Card><h2>Refund and credit note</h2><form className="admin-form" onSubmit={refund}>
              <select name="paymentMethodId" required><option value="">Refund method</option>{methods.filter((item) => item.code).map((item) => <option key={item.id} value={item.id}>{item.name}</option>)}</select>
              <input name="amount" type="number" min="0.01" max={detail.sale.paidTotal} step="0.01" placeholder="Amount" required />
              <input name="reason" placeholder="Required reason" required /><input name="reference" placeholder="Reference" />
              <Button disabled={Boolean(working)}>Issue refund</Button>
            </form></Card>}
            {detail.sale.status !== "Posted" && detail.sale.customerId && <Card><h2>Apply customer deposit</h2><form className="admin-form" onSubmit={applyDeposit}>
              <select name="depositId" required><option value="">Available deposit</option>{deposits.map((item) => <option key={item.id} value={item.id}>{item.depositNumber} - {money(item.availableAmount)}</option>)}</select>
              <input name="amount" type="number" min="0.01" max={detail.sale.balanceDue} step="0.01" placeholder="Amount" required />
              <Button disabled={Boolean(working || deposits.length === 0)}>Apply deposit</Button>
            </form></Card>}
            {detail.sale.status !== "Posted" && <Card><h2>Redeem gift card</h2><form className="admin-form" onSubmit={redeemGift}>
              <input name="code" placeholder="Gift-card code" autoComplete="off" required />
              <input name="amount" type="number" min="0.01" max={detail.sale.balanceDue} step="0.01" placeholder="Amount" required />
              <Button disabled={Boolean(working)}>Redeem</Button>
            </form></Card>}
            {detail.sale.status === "Posted" && detail.sale.customerId && <Card><h2>Activate package</h2><form className="admin-form" onSubmit={activatePackage}>
              <select name="packageId" required><option value="">Package definition</option>{packages.map((item) => <option key={item.id} value={item.id}>{item.name}</option>)}</select>
              <Button disabled={Boolean(working)}>Activate from sale</Button>
            </form></Card>}
            {detail.sale.status === "Posted" && detail.sale.customerId && <Card><h2>Enroll membership</h2><form className="admin-form" onSubmit={enrollMembership}>
              <select name="planId" required><option value="">Membership plan</option>{memberships.map((item) => <option key={item.id} value={item.id}>{item.name}</option>)}</select>
              <Button disabled={Boolean(working)}>Enroll from sale</Button>
            </form></Card>}
          </div>
        </>
      )}
    </PortalShell>
  );
}
