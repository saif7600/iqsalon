"use client";
import { useState } from "react";
import { apiRequest } from "@atiqsalon/sdk";
import { Button, FormField, Input } from "@atiqsalon/ui";
export function AuthForm({
  mode,
}: {
  mode: "login" | "register" | "forgot" | "reset" | "verify";
}) {
  const [message, setMessage] = useState("");
  const [submitting, setSubmitting] = useState(false);
  const submit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (mode !== "login" && mode !== "register") {
      setMessage("This account workflow is not available yet.");
      return;
    }
    setSubmitting(true);
    setMessage("");
    const data = new FormData(event.currentTarget);
    try {
      await apiRequest(mode === "login" ? "/auth/login" : "/auth/register", {
        method: "POST",
        body: JSON.stringify({
          email: data.get("email"),
          password: data.get("password"),
          ...(mode === "register"
            ? {
                displayName: data.get("displayName"),
                organizationName: data.get("organizationName"),
                countryCode: "AE",
                currency: "AED",
                language: "en",
                timeZone: "Asia/Dubai",
              }
            : {}),
        }),
      });
      if (mode === "register") {
        setMessage(
          "Registration accepted. Verify your email before signing in.",
        );
        setSubmitting(false);
      } else {
        const returnUrl = new URLSearchParams(window.location.search).get(
          "returnUrl",
        );
        window.location.assign(
          returnUrl?.startsWith("/") ? returnUrl : "/calendar",
        );
      }
    } catch {
      setMessage(
        mode === "login"
          ? "The email or password is incorrect."
          : "Registration could not be completed. Check the details or use another email.",
      );
      setSubmitting(false);
    }
  };
  const title =
    mode === "login"
      ? "Welcome back"
      : mode === "register"
        ? "Create your account"
        : mode === "forgot"
          ? "Reset your password"
          : mode === "verify"
            ? "Verify your email"
            : "Choose a new password";
  return (
    <form className="auth-form" onSubmit={submit}>
      <a className="wordmark" href="/">
        AtiqSalon AI
      </a>
      <h2>{title}</h2>
      {mode !== "verify" && (
        <FormField label="Email address" htmlFor="email">
          <Input
            id="email"
            name="email"
            type="email"
            autoComplete="email"
            required
          />
        </FormField>
      )}
      {mode === "register" && (
        <>
          <FormField label="Your name" htmlFor="displayName">
            <Input id="displayName" name="displayName" required />
          </FormField>
          <FormField label="Organization name" htmlFor="organizationName">
            <Input id="organizationName" name="organizationName" required />
          </FormField>
        </>
      )}
      {(mode === "login" || mode === "register" || mode === "reset") && (
        <FormField label="Password" htmlFor="password">
          <Input
            id="password"
            name="password"
            type="password"
            minLength={12}
            autoComplete={
              mode === "login" ? "current-password" : "new-password"
            }
            required
          />
        </FormField>
      )}
      <Button type="submit" disabled={submitting}>
        {submitting
          ? "Please wait..."
          : mode === "login"
            ? "Sign in"
            : mode === "register"
              ? "Create account"
              : "Continue"}
      </Button>
      {message && (
        <p role="status" className="notice">
          {message}
        </p>
      )}
      {mode === "login" && (
        <p>
          <a href="/register">Create account</a>
        </p>
      )}
    </form>
  );
}
