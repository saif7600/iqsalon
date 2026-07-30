"use client";
import { FormEvent, useState } from "react";
import { apiRequest } from "@atiqsalon/sdk";
import { Button, Card, ErrorState, PageTitle } from "@atiqsalon/ui";
import { PortalShell } from "./portal-shell";
export function GrowthWorkspace() {
  const [customerId, setCustomerId] = useState("");
  const [account, setAccount] = useState<{
    pointsBalance: number;
    lifetimePoints: number;
  } | null>(null);
  const [error, setError] = useState("");
  async function lookup(event: FormEvent) {
    event.preventDefault();
    setError("");
    try {
      setAccount(await apiRequest(`/loyalty/accounts/${customerId}`));
    } catch {
      setAccount(null);
      setError("No loyalty account was found for that customer.");
    }
  }
  return (
    <PortalShell title="Loyalty & referrals">
      <PageTitle eyebrow="Customer growth" title="Loyalty & referrals">
        Live loyalty balances with consent-safe referral governance.
      </PageTitle>
      {error && <ErrorState message={error} />}
      <div className="phase-grid">
        <Card>
          <h2>Loyalty lookup</h2>
          <form className="stack-form" onSubmit={lookup}>
            <label>
              Customer ID
              <input
                value={customerId}
                onChange={(e) => setCustomerId(e.target.value)}
                required
              />
            </label>
            <Button type="submit">Find account</Button>
          </form>
          {account && (
            <p>
              <strong>{account.pointsBalance} points available</strong>
              <br />
              {account.lifetimePoints} lifetime points
            </p>
          )}
        </Card>
        <Card>
          <h2>Referral governance</h2>
          <p>
            Each referred customer can qualify once. Reward automation remains
            disabled until its accounting policy is approved.
          </p>
        </Card>
        <Card>
          <h2>Responsible growth</h2>
          <p>
            Loyalty and referral participation never grants marketing consent.
          </p>
        </Card>
      </div>
    </PortalShell>
  );
}
