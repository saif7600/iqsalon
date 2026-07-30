"use client";

import { FormEvent, useCallback, useEffect, useState } from "react";
import { apiRequest } from "@atiqsalon/sdk";
import { Badge, Button, Card, ErrorState, LoadingState, PageTitle } from "@atiqsalon/ui";
import { PortalShell } from "./portal-shell";

type Branch = { id: string; organizationId: string; name: string };
type Service = { id: string; name: string };
type NamedRecord = { id: string; code: string; name: string };
type Till = { id: string; status: string; openingFloat: number; expectedCash: number };
type Closing = { id: string; businessDate: string; status: string; netSales: number; cashVariance: number };
type GiftIssue = { id: string; number: string; code: string; lastFour: string };

const today = new Date().toISOString().slice(0, 10);

export function CommercialAdminWorkspace() {
  const [branches, setBranches] = useState<Branch[]>([]);
  const [services, setServices] = useState<Service[]>([]);
  const [methods, setMethods] = useState<NamedRecord[]>([]);
  const [packages, setPackages] = useState<NamedRecord[]>([]);
  const [plans, setPlans] = useState<NamedRecord[]>([]);
  const [commissions, setCommissions] = useState<NamedRecord[]>([]);
  const [branchId, setBranchId] = useState("");
  const [till, setTill] = useState<Till | null>(null);
  const [closings, setClosings] = useState<Closing[]>([]);
  const [gift, setGift] = useState<GiftIssue | null>(null);
  const [loading, setLoading] = useState(true);
  const [working, setWorking] = useState("");
  const [error, setError] = useState("");
  const [message, setMessage] = useState("");

  const refreshBranchState = useCallback(async (selectedBranch: string) => {
    if (!selectedBranch) return;
    const [currentTill, closingRows] = await Promise.all([
      apiRequest<Till | null>(`/till-sessions/current?branchId=${selectedBranch}`),
      apiRequest<Closing[]>(`/daily-closings?branchId=${selectedBranch}`),
    ]);
    setTill(currentTill);
    setClosings(closingRows);
  }, []);

  const refreshCatalogues = useCallback(async () => {
    const [paymentMethods, packageRows, membershipRows, commissionRows] = await Promise.all([
      apiRequest<NamedRecord[]>("/payment-methods"),
      apiRequest<NamedRecord[]>("/packages"),
      apiRequest<NamedRecord[]>("/membership-plans"),
      apiRequest<NamedRecord[]>("/commission-plans"),
    ]);
    setMethods(paymentMethods);
    setPackages(packageRows);
    setPlans(membershipRows);
    setCommissions(commissionRows);
  }, []);

  useEffect(() => {
    async function initialize() {
      try {
        const [availableBranches, availableServices] = await Promise.all([
          apiRequest<Branch[]>("/branches"),
          apiRequest<Service[]>("/services"),
        ]);
        setBranches(availableBranches);
        setServices(availableServices);
        const first = availableBranches[0]?.id ?? "";
        setBranchId(first);
        await Promise.all([refreshCatalogues(), refreshBranchState(first)]);
      } catch {
        setError("Commercial administration data could not be loaded for this account.");
      } finally {
        setLoading(false);
      }
    }
    void initialize();
  }, [refreshBranchState, refreshCatalogues]);

  const branch = branches.find((item) => item.id === branchId);

  async function mutate(label: string, action: () => Promise<unknown>, refresh?: () => Promise<void>) {
    setWorking(label);
    setError("");
    setMessage("");
    try {
      await action();
      if (refresh) await refresh();
      setMessage(`${label} completed.`);
    } catch {
      setError(`${label} was rejected. Check your permission and the submitted values.`);
    } finally {
      setWorking("");
    }
  }

  function paymentMethod(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!branch) return;
    const data = new FormData(event.currentTarget);
    void mutate("Payment method setup", () => apiRequest("/payment-methods", {
      method: "POST",
      body: JSON.stringify({
        organizationId: branch.organizationId,
        code: data.get("code"),
        name: data.get("name"),
        type: data.get("type"),
        requiresReference: data.get("requiresReference") === "on",
        requiresTillSession: data.get("requiresTillSession") === "on",
        supportsRefund: true,
        supportsChange: data.get("type") === "Cash",
        supportsPartialPayment: true,
        isActive: true,
        displayOrder: methods.length,
      }),
    }), refreshCatalogues);
  }

  function packagePlan(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!branch) return;
    const data = new FormData(event.currentTarget);
    void mutate("Package setup", () => apiRequest("/packages", {
      method: "POST",
      body: JSON.stringify({
        organizationId: branch.organizationId,
        code: data.get("code"),
        name: data.get("name"),
        description: data.get("description"),
        price: Number(data.get("price")),
        validityDays: Number(data.get("validityDays")),
        entitlements: [{ serviceId: data.get("serviceId"), quantity: Number(data.get("quantity")) }],
      }),
    }), refreshCatalogues);
  }

  function membershipPlan(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!branch) return;
    const data = new FormData(event.currentTarget);
    void mutate("Membership plan setup", () => apiRequest("/membership-plans", {
      method: "POST",
      body: JSON.stringify({
        organizationId: branch.organizationId,
        code: data.get("code"),
        name: data.get("name"),
        description: data.get("description"),
        recurringPrice: Number(data.get("price")),
        billingInterval: data.get("interval"),
        includedCredits: Number(data.get("credits")),
      }),
    }), refreshCatalogues);
  }

  function commissionPlan(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!branch) return;
    const data = new FormData(event.currentTarget);
    void mutate("Commission plan setup", () => apiRequest("/commission-plans", {
      method: "POST",
      body: JSON.stringify({
        organizationId: branch.organizationId,
        code: data.get("code"),
        name: data.get("name"),
        basis: data.get("basis"),
        serviceRatePercentage: Number(data.get("serviceRate")),
        productRatePercentage: Number(data.get("productRate")),
        includeTips: false,
      }),
    }), refreshCatalogues);
  }

  function issueGiftCard(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!branch) return;
    const data = new FormData(event.currentTarget);
    setGift(null);
    void mutate("Gift-card issuance", async () => {
      const issued = await apiRequest<GiftIssue>("/gift-cards", {
        method: "POST",
        body: JSON.stringify({
          organizationId: branch.organizationId,
          branchId: branch.id,
          saleId: data.get("saleId"),
          value: Number(data.get("value")),
          currencyCode: "AED",
          customerId: data.get("customerId") || null,
          expiresAtUtc: data.get("expiresAt") ? new Date(String(data.get("expiresAt"))).toISOString() : null,
        }),
      });
      setGift(issued);
    });
  }

  function openTill(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!branch) return;
    const data = new FormData(event.currentTarget);
    void mutate("Till opening", () => apiRequest("/till-sessions/open", {
      method: "POST",
      body: JSON.stringify({
        organizationId: branch.organizationId,
        branchId: branch.id,
        openingFloat: Number(data.get("openingFloat")),
      }),
    }), () => refreshBranchState(branch.id));
  }

  function closeTill(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!branch || !till) return;
    const data = new FormData(event.currentTarget);
    void mutate("Till closing", () => apiRequest(`/till-sessions/${till.id}/close`, {
      method: "POST",
      body: JSON.stringify({ countedCash: Number(data.get("countedCash")) }),
    }), () => refreshBranchState(branch.id));
  }

  function dailyClosing(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!branch) return;
    const data = new FormData(event.currentTarget);
    void mutate("Daily closing", () => apiRequest("/daily-closings", {
      method: "POST",
      body: JSON.stringify({
        organizationId: branch.organizationId,
        branchId: branch.id,
        businessDate: data.get("businessDate"),
      }),
    }), () => refreshBranchState(branch.id));
  }

  return (
    <PortalShell title="Commercial administration">
      <PageTitle eyebrow="Controlled operations" title="Commercial administration">
        Configure commercial ledgers and run branch close operations through authorized API workflows.
      </PageTitle>
      {error && <ErrorState message={error} />}
      {message && <p className="notice">{message}</p>}
      {loading ? <LoadingState /> : (
        <>
          <div className="commercial-context">
            <label>Operating branch<select value={branchId} onChange={(event) => {
              setBranchId(event.target.value);
              void refreshBranchState(event.target.value);
            }}>{branches.map((item) => <option key={item.id} value={item.id}>{item.name}</option>)}</select></label>
            <span><Badge>{methods.length} payment methods</Badge><Badge>{packages.length} packages</Badge><Badge>{plans.length} memberships</Badge><Badge>{commissions.length} commission plans</Badge></span>
          </div>
          <div className="admin-grid">
            <Card><h2>Payment method</h2><form className="admin-form" onSubmit={paymentMethod}>
              <input name="code" placeholder="Code" required /><input name="name" placeholder="Display name" required />
              <select name="type"><option>Cash</option><option>Card</option><option>BankTransfer</option><option>GiftCard</option><option>Other</option></select>
              <label className="check"><input name="requiresReference" type="checkbox" /> Require reference</label>
              <label className="check"><input name="requiresTillSession" type="checkbox" /> Require till</label>
              <Button disabled={Boolean(working)}>Create method</Button>
            </form></Card>
            <Card><h2>Package</h2><form className="admin-form" onSubmit={packagePlan}>
              <input name="code" placeholder="Code" required /><input name="name" placeholder="Name" required />
              <input name="description" placeholder="Description" /><input name="price" type="number" min="0" step="0.01" placeholder="Price" required />
              <input name="validityDays" type="number" min="1" defaultValue="365" required />
              <select name="serviceId" required><option value="">Entitled service</option>{services.map((item) => <option key={item.id} value={item.id}>{item.name}</option>)}</select>
              <input name="quantity" type="number" min="0.01" step="0.01" defaultValue="1" required />
              <Button disabled={Boolean(working)}>Create package</Button>
            </form></Card>
            <Card><h2>Membership plan</h2><form className="admin-form" onSubmit={membershipPlan}>
              <input name="code" placeholder="Code" required /><input name="name" placeholder="Name" required />
              <input name="description" placeholder="Description" /><input name="price" type="number" min="0" step="0.01" placeholder="Recurring price" required />
              <select name="interval"><option>Monthly</option><option>Weekly</option><option>Quarterly</option><option>Annual</option></select>
              <input name="credits" type="number" min="0" step="0.01" placeholder="Included credits" required />
              <Button disabled={Boolean(working)}>Create membership</Button>
            </form></Card>
            <Card><h2>Commission plan</h2><form className="admin-form" onSubmit={commissionPlan}>
              <input name="code" placeholder="Code" required /><input name="name" placeholder="Name" required />
              <select name="basis"><option>NetRevenue</option><option>GrossRevenue</option><option>GrossProfit</option></select>
              <input name="serviceRate" type="number" min="0" max="100" step="0.01" placeholder="Service %" required />
              <input name="productRate" type="number" min="0" max="100" step="0.01" placeholder="Product %" required />
              <Button disabled={Boolean(working)}>Create commission plan</Button>
            </form></Card>
            <Card><h2>Gift-card issuance</h2><form className="admin-form" onSubmit={issueGiftCard}>
              <input name="saleId" placeholder="Posted sale ID" required /><input name="customerId" placeholder="Customer ID (optional)" />
              <input name="value" type="number" min="0.01" step="0.01" placeholder="Value" required />
              <input name="expiresAt" type="date" /><Button disabled={Boolean(working)}>Issue gift card</Button>
            </form>{gift && <div className="secret-result"><small>Display once</small><strong>{gift.number}</strong><code>{gift.code}</code></div>}</Card>
            <Card><h2>Till session</h2>{till ? <form className="admin-form" onSubmit={closeTill}>
              <p><Badge>{till.status}</Badge> Expected {till.expectedCash.toFixed(2)}</p>
              <input name="countedCash" type="number" min="0" step="0.01" placeholder="Counted cash" required />
              <Button disabled={Boolean(working)}>Close till</Button>
            </form> : <form className="admin-form" onSubmit={openTill}>
              <input name="openingFloat" type="number" min="0" step="0.01" placeholder="Opening float" required />
              <Button disabled={Boolean(working)}>Open till</Button>
            </form>}</Card>
            <Card><h2>Daily closing</h2><form className="admin-form" onSubmit={dailyClosing}>
              <input name="businessDate" type="date" defaultValue={today} required />
              <Button disabled={Boolean(working || till)}>Create closing</Button>
            </form>{closings.slice(0, 4).map((closing) => <div className="report-row" key={closing.id}><span>{closing.businessDate}<small>Variance {closing.cashVariance.toFixed(2)}</small></span><Badge>{closing.status}</Badge></div>)}</Card>
          </div>
        </>
      )}
    </PortalShell>
  );
}
