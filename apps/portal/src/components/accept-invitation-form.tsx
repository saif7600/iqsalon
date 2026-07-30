"use client";

import { FormEvent, useState } from "react";
import { useSearchParams } from "next/navigation";
import { Button, FormField, Input } from "@atiqsalon/ui";

export function AcceptInvitationForm() {
  const token = useSearchParams().get("token") ?? "";
  const [message, setMessage] = useState("");
  const [error, setError] = useState("");
  const [submitting, setSubmitting] = useState(false);
  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); setSubmitting(true); setError("");
    const data = new FormData(event.currentTarget);
    if (data.get("password") !== data.get("confirmPassword")) { setError("Passwords do not match."); setSubmitting(false); return; }
    const response = await fetch("/api/v1/auth/accept-invitation", { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ token, password: data.get("password") }) });
    const payload = await response.json();
    if (!response.ok) setError(payload?.errors?.invitation?.[0] ?? payload?.errors?.password?.[0] ?? "Invitation could not be accepted.");
    else setMessage(payload.message);
    setSubmitting(false);
  }
  return <form className="auth-form" onSubmit={submit}><div><p className="eyebrow">Tenant owner invitation</p><h1>Activate your account</h1><p>Choose a secure password to access your AtiqSalon workspace.</p></div>{error && <p className="error-state" role="alert">{error}</p>}{message ? <><p className="notice">{message}</p><a className="button" href="/login">Continue to sign in</a></> : <><FormField label="Password" htmlFor="password" hint="Use at least 12 characters." required><Input id="password" name="password" type="password" minLength={12} autoComplete="new-password" required /></FormField><FormField label="Confirm password" htmlFor="confirmPassword" required><Input id="confirmPassword" name="confirmPassword" type="password" minLength={12} autoComplete="new-password" required /></FormField><Button type="submit" disabled={submitting || !token}>{submitting ? "Activating..." : "Activate account"}</Button>{!token && <p className="error-state">The invitation token is missing.</p>}</>}</form>;
}
