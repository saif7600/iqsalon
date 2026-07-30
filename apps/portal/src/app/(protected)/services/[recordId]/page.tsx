import { ServiceResourceProfile } from "@/components/management-detail-workspace";

export default async function ServiceDetailPage({
  params,
}: {
  params: Promise<{ recordId: string }>;
}) {
  const { recordId } = await params;
  return <ServiceResourceProfile recordId={recordId} />;
}
