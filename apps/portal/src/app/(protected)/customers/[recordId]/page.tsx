import { CustomerCrmProfile } from "@/components/management-detail-workspace";

export default async function CustomerDetailPage({
  params,
}: {
  params: Promise<{ recordId: string }>;
}) {
  const { recordId } = await params;
  return <CustomerCrmProfile recordId={recordId} />;
}
