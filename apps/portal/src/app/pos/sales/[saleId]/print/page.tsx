import { FinancialDocumentView } from "@/components/financial-document-view";

export default async function PrintSalePage({ params }: { params: Promise<{ saleId: string }> }) {
  const { saleId } = await params;
  return <FinancialDocumentView saleId={saleId} />;
}
