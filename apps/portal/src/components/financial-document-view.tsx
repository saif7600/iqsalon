"use client";

import { useEffect, useState } from "react";
import { apiRequest } from "@atiqsalon/sdk";
import { Button, ErrorState, LoadingState } from "@atiqsalon/ui";

type Documents = {
  sale: { saleNumber: string; businessDate: string; subtotal: number; discountTotal: number; taxTotal: number; grandTotal: number };
  invoice?: { invoiceNumber: string };
  creditNotes: Array<{ id: string; creditNoteNumber: string; grandTotal: number; reason: string }>;
  lines: Array<{ id: string; descriptionSnapshot: string; quantity: number; unitPrice: number; taxAmount: number; lineTotal: number }>;
};
const money = (value: number) => new Intl.NumberFormat("en-AE", { style: "currency", currency: "AED" }).format(value);

export function FinancialDocumentView({ saleId }: { saleId: string }) {
  const [documents, setDocuments] = useState<Documents | null>(null);
  const [error, setError] = useState("");
  useEffect(() => {
    apiRequest<Documents>(`/sales/${saleId}/financial-documents`).then(setDocuments)
      .catch(() => setError("Financial documents could not be loaded."));
  }, [saleId]);
  if (error) return <ErrorState message={error} />;
  if (!documents) return <LoadingState />;
  return <main className="print-document">
    <header><div><small>AtiqSalon AI</small><h1>{documents.invoice ? "Tax invoice" : "Sale statement"}</h1></div>
      <Button className="print-button" onClick={() => window.print()}>Print</Button></header>
    <section className="print-meta"><div><small>Document</small><strong>{documents.invoice?.invoiceNumber ?? documents.sale.saleNumber}</strong></div>
      <div><small>Business date</small><strong>{documents.sale.businessDate}</strong></div></section>
    <table><thead><tr><th>Description</th><th>Qty</th><th>Unit</th><th>VAT</th><th>Total</th></tr></thead>
      <tbody>{documents.lines.map((line) => <tr key={line.id}><td>{line.descriptionSnapshot}</td><td>{line.quantity}</td><td>{money(line.unitPrice)}</td><td>{money(line.taxAmount)}</td><td>{money(line.lineTotal)}</td></tr>)}</tbody></table>
    <dl className="print-totals"><div><dt>Subtotal</dt><dd>{money(documents.sale.subtotal)}</dd></div>
      <div><dt>Discounts</dt><dd>{money(documents.sale.discountTotal)}</dd></div><div><dt>VAT</dt><dd>{money(documents.sale.taxTotal)}</dd></div>
      <div><dt>Total</dt><dd>{money(documents.sale.grandTotal)}</dd></div></dl>
    {documents.creditNotes.map((note) => <section className="credit-note" key={note.id}><h2>Credit note {note.creditNoteNumber}</h2><p>{note.reason}</p><strong>{money(note.grandTotal)}</strong></section>)}
  </main>;
}
