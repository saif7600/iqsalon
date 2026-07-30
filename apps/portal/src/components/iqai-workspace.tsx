"use client";
import { FormEvent, useEffect, useState } from "react";
import { apiRequest } from "@atiqsalon/sdk";
import { Button, Card, ErrorState, PageTitle } from "@atiqsalon/ui";
import { PortalShell } from "./portal-shell";
type Status = { configured: boolean };
type Message = { role: "user" | "iqai"; text: string };
export function IqaiWorkspace() {
  const [status, setStatus] = useState<Status | null>(null);
  const [messages, setMessages] = useState<Message[]>([]);
  const [conversationId, setConversationId] = useState<string>();
  const [error, setError] = useState("");
  const [busy, setBusy] = useState(false);
  useEffect(() => {
    apiRequest<Status>("/iqai/status")
      .then(setStatus)
      .catch(() => setError("IQAI status is unavailable."));
  }, []);
  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const form = event.currentTarget;
    const text = String(new FormData(form).get("message") ?? "").trim();
    if (!text) return;
    setMessages((x) => [...x, { role: "user", text }]);
    setBusy(true);
    setError("");
    form.reset();
    try {
      const result = await apiRequest<{ conversationId: string; text: string }>(
        "/iqai/chat",
        {
          method: "POST",
          body: JSON.stringify({
            message: text,
            conversationId,
            languageCode: "en",
          }),
        },
      );
      setConversationId(result.conversationId);
      setMessages((x) => [...x, { role: "iqai", text: result.text }]);
    } catch (reason) {
      const detail =
        typeof reason === "object" && reason && "detail" in reason
          ? String(reason.detail)
          : "IQAI could not answer. Try again in a moment.";
      setError(detail);
    } finally {
      setBusy(false);
    }
  }
  return (
    <PortalShell title="IQAI">
      <PageTitle eyebrow="Advisory intelligence" title="Ask IQAI">
        IQAI can explain and recommend. It cannot change salon records.
      </PageTitle>
      {error && <ErrorState message={error} />}
      <div className="iqai-layout">
        <Card>
          <h2>Provider status</h2>
          <p className={`status-dot ${status?.configured ? "ready" : ""}`}>
            {status?.configured ? "Connection configured" : "Unavailable"}
          </p>
          <small>Advisory mode · writes disabled</small>
        </Card>
        <Card>
          <div className="chat-log" aria-live="polite">
            {messages.length ? (
              messages.map((m, i) => (
                <div className={`chat-message ${m.role}`} key={i}>
                  <strong>{m.role === "iqai" ? "IQAI" : "You"}</strong>
                  <p>{m.text}</p>
                </div>
              ))
            ) : (
              <p>
                Ask about scheduling, operations, performance, retention, or
                salon management.
              </p>
            )}
          </div>
          <form className="chat-form" onSubmit={submit}>
            <textarea
              name="message"
              maxLength={8000}
              required
              placeholder="Ask IQAI..."
            />
            <Button type="submit" disabled={busy || !status?.configured}>
              {busy ? "Thinking..." : "Send"}
            </Button>
          </form>
        </Card>
      </div>
    </PortalShell>
  );
}
