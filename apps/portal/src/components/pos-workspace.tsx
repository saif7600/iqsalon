"use client";

import { FormEvent, useEffect, useMemo, useState } from "react";
import { apiRequest } from "@atiqsalon/sdk";
import { Badge, Button, Card, ErrorState, LoadingState, PageTitle } from "@atiqsalon/ui";
import { PortalShell } from "./portal-shell";

type Branch = { id: string; organizationId: string; name: string };
type CatalogueItem = { id: string; name: string; basePrice?: number; retailPrice?: number; sku?: string };
type PaymentMethod = { id: string; name: string; code: string };
type SaleLine = { id: string; description: string; quantity: number; lineTotal: number };
type Sale = {
  id: string; saleNumber: string; status: string; subtotal: number; taxTotal: number;
  grandTotal: number; paidAmount: number; balanceDue: number; lines?: SaleLine[];
};
type SaleDetail = { sale: Sale; lines: SaleLine[] };
type DraftLine = { key: string; itemType: "Service" | "Product"; itemId: string; quantity: number };

function money(value: number) {
  return new Intl.NumberFormat("en-AE", { style: "currency", currency: "AED" }).format(value);
}

export function PosWorkspace({ appointmentId }: { appointmentId?: string }) {
  const [branches, setBranches] = useState<Branch[]>([]);
  const [services, setServices] = useState<CatalogueItem[]>([]);
  const [products, setProducts] = useState<CatalogueItem[]>([]);
  const [methods, setMethods] = useState<PaymentMethod[]>([]);
  const [branchId, setBranchId] = useState("");
  const [draftLines, setDraftLines] = useState<DraftLine[]>([]);
  const [sale, setSale] = useState<Sale | null>(null);
  const [loading, setLoading] = useState(true);
  const [working, setWorking] = useState(false);
  const [error, setError] = useState("");

  useEffect(() => {
    async function load() {
      try {
        const [availableBranches, availableServices, availableProducts, availableMethods] = await Promise.all([
          apiRequest<Branch[]>("/branches"),
          apiRequest<CatalogueItem[]>("/services"),
          apiRequest<CatalogueItem[]>("/products"),
          apiRequest<PaymentMethod[]>("/payment-methods"),
        ]);
        setBranches(availableBranches);
        setBranchId(availableBranches[0]?.id ?? "");
        setServices(availableServices);
        setProducts(availableProducts);
        setMethods(availableMethods);
      } catch {
        setError("POS data could not be loaded. Check your session and branch permissions.");
      } finally {
        setLoading(false);
      }
    }
    void load();
  }, []);

  const selectedBranch = branches.find((branch) => branch.id === branchId);
  const estimatedTotal = useMemo(() => draftLines.reduce((total, line) => {
    const source = line.itemType === "Service" ? services : products;
    const item = source.find((candidate) => candidate.id === line.itemId);
    return total + (item?.basePrice ?? item?.retailPrice ?? 0) * line.quantity;
  }, 0), [draftLines, products, services]);

  function addLine(itemType: DraftLine["itemType"], itemId: string) {
    setDraftLines((current) => [...current, { key: crypto.randomUUID(), itemType, itemId, quantity: 1 }]);
  }

  async function refreshSale(id: string) {
    const detail = await apiRequest<SaleDetail>(`/sales/${id}`);
    setSale({ ...detail.sale, lines: detail.lines });
  }

  async function createSale() {
    if (!selectedBranch || draftLines.length === 0) return;
    setWorking(true);
    setError("");
    try {
      const created = await apiRequest<Sale>("/sales", {
        method: "POST",
        body: JSON.stringify({
          organizationId: selectedBranch.organizationId,
          branchId: selectedBranch.id,
          appointmentId: appointmentId ?? null,
          customerId: null,
          lines: draftLines.map((line) => ({
            itemType: line.itemType,
            serviceId: line.itemType === "Service" ? line.itemId : null,
            productId: line.itemType === "Product" ? line.itemId : null,
            quantity: line.quantity,
            discountAmount: 0,
          })),
        }),
      });
      await refreshSale(created.id);
    } catch {
      setError("The sale could not be created. Verify the branch catalogue and appointment state.");
    } finally {
      setWorking(false);
    }
  }

  async function recordPayment(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!sale) return;
    const data = new FormData(event.currentTarget);
    setWorking(true);
    setError("");
    try {
      await apiRequest(`/sales/${sale.id}/payments`, {
        method: "POST",
        body: JSON.stringify({
          paymentMethodId: data.get("paymentMethodId"),
          amount: Number(data.get("amount")),
          reference: data.get("reference") || null,
          idempotencyKey: crypto.randomUUID(),
          tillSessionId: null,
        }),
      });
      await refreshSale(sale.id);
    } catch {
      setError("The payment was rejected. Check the amount, method, and till requirements.");
    } finally {
      setWorking(false);
    }
  }

  async function postSale() {
    if (!sale) return;
    setWorking(true);
    setError("");
    try {
      await apiRequest(`/sales/${sale.id}/post`, {
        method: "POST",
        body: JSON.stringify({ idempotencyKey: crypto.randomUUID() }),
      });
      await refreshSale(sale.id);
    } catch {
      setError("Posting was rejected. The sale must be fully settled before an invoice is issued.");
    } finally {
      setWorking(false);
    }
  }

  return (
    <PortalShell title="Point of sale">
      <PageTitle eyebrow="Commercial operations" title="Point of sale">
        Branch-scoped checkout with server-owned pricing, VAT, payment allocation, and invoice posting.
      </PageTitle>
      {error && <ErrorState message={error} />}
      {loading ? <LoadingState /> : (
        <div className="pos-layout">
          <section className="pos-catalogue">
            <div className="pos-toolbar">
              <label>Branch
                <select value={branchId} disabled={Boolean(sale)} onChange={(event) => setBranchId(event.target.value)}>
                  {branches.map((branch) => <option key={branch.id} value={branch.id}>{branch.name}</option>)}
                </select>
              </label>
              {appointmentId && <Badge>Appointment checkout</Badge>}
            </div>
            <Card>
              <h2>Services</h2>
              <div className="pos-items">
                {services.map((item) => (
                  <button key={item.id} disabled={Boolean(sale)} onClick={() => addLine("Service", item.id)}>
                    <strong>{item.name}</strong><span>{money(item.basePrice ?? 0)}</span>
                  </button>
                ))}
              </div>
            </Card>
            <Card>
              <h2>Products</h2>
              <div className="pos-items">
                {products.length === 0 ? <p className="muted">No active branch products are configured.</p> : products.map((item) => (
                  <button key={item.id} disabled={Boolean(sale)} onClick={() => addLine("Product", item.id)}>
                    <strong>{item.name}</strong><span>{item.sku} | {money(item.retailPrice ?? 0)}</span>
                  </button>
                ))}
              </div>
            </Card>
          </section>

          <aside className="pos-ticket">
            <Card>
              <div className="record-heading">
                <div><small>{sale?.saleNumber ?? "New basket"}</small><h2>Checkout</h2></div>
                <Badge>{sale?.status ?? "Drafting"}</Badge>
              </div>
              <div className="ticket-lines">
                {(sale?.lines ?? draftLines).map((line) => {
                  if ("description" in line) return <div key={line.id}><span>{line.description} x {line.quantity}</span><strong>{money(line.lineTotal)}</strong></div>;
                  const source = line.itemType === "Service" ? services : products;
                  const item = source.find((candidate) => candidate.id === line.itemId);
                  return <div key={line.key}><span>{item?.name ?? line.itemType} x {line.quantity}</span><strong>{money((item?.basePrice ?? item?.retailPrice ?? 0) * line.quantity)}</strong></div>;
                })}
              </div>
              <dl className="ticket-totals">
                {sale && <><div><dt>Subtotal</dt><dd>{money(sale.subtotal)}</dd></div><div><dt>VAT</dt><dd>{money(sale.taxTotal)}</dd></div></>}
                <div className="grand"><dt>Total</dt><dd>{money(sale?.grandTotal ?? estimatedTotal)}</dd></div>
                {sale && <><div><dt>Paid</dt><dd>{money(sale.paidAmount)}</dd></div><div><dt>Balance</dt><dd>{money(sale.balanceDue)}</dd></div></>}
              </dl>
              {!sale ? (
                <Button disabled={working || draftLines.length === 0 || !branchId} onClick={createSale}>Create sale</Button>
              ) : sale.status !== "Posted" ? (
                <>
                  {methods.length === 0 ? <p className="notice">No payment methods are configured. An administrator must configure one before settlement.</p> : (
                    <form className="payment-form" onSubmit={recordPayment}>
                      <select name="paymentMethodId" required>{methods.map((method) => <option key={method.id} value={method.id}>{method.name}</option>)}</select>
                      <input name="amount" type="number" min="0.01" step="0.01" defaultValue={sale.balanceDue} required />
                      <input name="reference" placeholder="Reference (optional)" />
                      <Button type="submit" disabled={working || sale.balanceDue <= 0}>Record payment</Button>
                    </form>
                  )}
                  <Button className="secondary" disabled={working || sale.balanceDue > 0} onClick={postSale}>Post and issue invoice</Button>
                </>
              ) : <p className="notice">Sale posted. The financial snapshot is immutable.</p>}
            </Card>
          </aside>
        </div>
      )}
    </PortalShell>
  );
}
