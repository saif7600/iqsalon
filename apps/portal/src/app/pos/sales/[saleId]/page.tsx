import { SaleOperationsWorkspace } from "@/components/sale-operations-workspace";

export default async function SalePage({ params }: { params: Promise<{ saleId: string }> }) {
  const { saleId } = await params;
  return <SaleOperationsWorkspace saleId={saleId} />;
}
