import { StaffOperatingProfile } from "@/components/management-detail-workspace";

export default async function StaffDetailPage({
  params,
}: {
  params: Promise<{ recordId: string }>;
}) {
  const { recordId } = await params;
  return <StaffOperatingProfile recordId={recordId} />;
}
